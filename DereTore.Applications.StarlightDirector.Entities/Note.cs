﻿using System;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DereTore.Applications.StarlightDirector.Entities {
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy), MemberSerialization = MemberSerialization.OptIn)]
    public sealed class Note : DependencyObject, IComparable<Note> {

        public event EventHandler<EventArgs> ExtraParamsChanged;

        [JsonProperty]
        public int ID { get; private set; }

        // "PositionInGrid" was the first name of this property used in serialization.
        [JsonProperty("positionInGrid")]
        public int IndexInGrid { get; set; }

        public NoteType Type {
            get { return (NoteType)GetValue(TypeProperty); }
            private set { SetValue(TypeProperty, value); }
        }

        [JsonProperty]
        public NotePosition StartPosition {
            get { return (NotePosition)GetValue(StartPositionProperty); }
            set { SetValue(StartPositionProperty, value); }
        }

        [JsonProperty]
        public NotePosition FinishPosition {
            get { return (NotePosition)GetValue(FinishPositionProperty); }
            set { SetValue(FinishPositionProperty, value); }
        }

        public int IndexInTrack => (int)FinishPosition - 1;

        [JsonProperty]
        public NoteFlickType FlickType {
            get { return (NoteFlickType)GetValue(FlickTypeProperty); }
            set { SetValue(FlickTypeProperty, value); }
        }

        public bool IsSync {
            get { return (bool)GetValue(IsSyncProperty); }
            private set { SetValue(IsSyncProperty, value); }
        }

        public Bar Bar { get; internal set; }

        public bool IsFlick {
            get { return (bool)GetValue(IsFlickProperty); }
            private set { SetValue(IsFlickProperty, value); }
        }

        [JsonProperty]
        public int PrevFlickNoteID { get; internal set; }

        public Note PrevFlickNote {
            get {
                return _prevFlickNote;
            }
            set {
                Note n1 = PrevFlickNote, n2 = NextFlickNote;
                if (n1 == null && n2 == null && value != null) {
                    FlickType = FinishPosition >= NotePosition.Center ? NoteFlickType.FlickRight : NoteFlickType.FlickLeft;
                }
                _prevFlickNote = value;
                n1 = PrevFlickNote;
                n2 = NextFlickNote;
                if (n1 == null && n2 == null) {
                    if (!IsHoldStart) {
                        FlickType = NoteFlickType.Tap;
                    }
                } else {
                    // Currently there isn't an example of quick 'Z' turn appeared in CGSS (as shown below),
                    // so the following completion method is good enough.
                    //     -->
                    //      ^
                    //       \
                    //      -->
                    if (n2 != null) {
                        FlickType = n2.FinishPosition > FinishPosition ? NoteFlickType.FlickRight : NoteFlickType.FlickLeft;
                    } else {
                        FlickType = n1.FinishPosition > FinishPosition ? NoteFlickType.FlickLeft : NoteFlickType.FlickRight;
                    }
                }
                PrevFlickNoteID = value?.ID ?? EntityID.Invalid;
            }
        }

        public bool HasPrevFlick => Type == NoteType.TapOrFlick && PrevFlickNote != null;

        [JsonProperty]
        public int NextFlickNoteID { get; internal set; }

        public Note NextFlickNote {
            get {
                return _nextFlickNote;
            }
            set {
                Note n1 = PrevFlickNote, n2 = NextFlickNote;
                if (n1 == null && n2 == null && value != null) {
                    FlickType = FinishPosition >= NotePosition.Center ? NoteFlickType.FlickRight : NoteFlickType.FlickLeft;
                }
                _nextFlickNote = value;
                n1 = PrevFlickNote;
                n2 = NextFlickNote;
                if (n1 == null && n2 == null) {
                    if (!IsHoldStart) {
                        FlickType = NoteFlickType.Tap;
                    }
                } else {
                    if (n2 != null) {
                        FlickType = n2.FinishPosition > FinishPosition ? NoteFlickType.FlickRight : NoteFlickType.FlickLeft;
                    } else {
                        FlickType = n1.FinishPosition > FinishPosition ? NoteFlickType.FlickLeft : NoteFlickType.FlickRight;
                    }
                }
                NextFlickNoteID = value?.ID ?? EntityID.Invalid;
            }
        }

        public bool HasNextFlick => Type == NoteType.TapOrFlick && NextFlickNote != null;

        [JsonProperty]
        public int SyncTargetID { get; internal set; }

        public Note SyncTarget {
            get {
                return _syncTarget;
            }
            set {
                var origSyncTarget = _syncTarget;
                _syncTarget = value;
                if (origSyncTarget?.SyncTarget != null && origSyncTarget.SyncTarget.Equals(this)) {
                    origSyncTarget.SyncTarget = null;
                }
                IsSync = value != null;
                SyncTargetID = value?.ID ?? EntityID.Invalid;
            }
        }

        public bool IsHold {
            get { return (bool)GetValue(IsHoldProperty); }
            private set { SetValue(IsHoldProperty, value); }
        }

        public bool IsHoldStart => Type == NoteType.Hold && HoldTarget != null;

        public bool IsHoldEnd => Type == NoteType.TapOrFlick && HoldTarget != null;

        [JsonProperty]
        public int HoldTargetID { get; internal set; }

        public Note HoldTarget {
            get {
                return _holdTarget;
            }
            set {
                var origHoldTarget = _holdTarget;
                _holdTarget = value;
                if (origHoldTarget?.HoldTarget != null && origHoldTarget.HoldTarget.Equals(this)) {
                    origHoldTarget.HoldTarget = null;
                }
                IsHold = value != null;
                // Only the former of the hold pair is considered as a hold note. The other is a tap or flick note.
                Type = (value != null && value > this) ? NoteType.Hold : NoteType.TapOrFlick;
                HoldTargetID = value?.ID ?? EntityID.Invalid;
            }
        }

        public bool IsTap => Type == NoteType.TapOrFlick && FlickType == NoteFlickType.Tap;

        public bool IsGamingNote => IsTypeGaming(Type);

        public bool IsSpecialNote => IsTypeSpecial(Type);

        public NoteExtraParams ExtraParams {
            get { return (NoteExtraParams)GetValue(ExtraParamsProperty); }
            set { SetValue(ExtraParamsProperty, value); }
        }

        public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(nameof(Type), typeof(NoteType), typeof(Note),
            new PropertyMetadata(NoteType.Invalid, OnTypeChanged));

        public static readonly DependencyProperty FlickTypeProperty = DependencyProperty.Register(nameof(FlickType), typeof(NoteFlickType), typeof(Note),
            new PropertyMetadata(NoteFlickType.Tap, OnFlickTypeChanged));

        public static readonly DependencyProperty IsSyncProperty = DependencyProperty.Register(nameof(IsSync), typeof(bool), typeof(Note),
            new PropertyMetadata(false));

        public static readonly DependencyProperty IsFlickProperty = DependencyProperty.Register(nameof(IsFlick), typeof(bool), typeof(Note),
            new PropertyMetadata(false));

        public static readonly DependencyProperty IsHoldProperty = DependencyProperty.Register(nameof(IsHold), typeof(bool), typeof(Note),
            new PropertyMetadata(false));

        public static readonly DependencyProperty StartPositionProperty = DependencyProperty.Register(nameof(StartPosition), typeof(NotePosition), typeof(Note),
            new PropertyMetadata(NotePosition.Nowhere));

        public static readonly DependencyProperty FinishPositionProperty = DependencyProperty.Register(nameof(FinishPosition), typeof(NotePosition), typeof(Note),
            new PropertyMetadata(NotePosition.Nowhere));

        public static readonly DependencyProperty ExtraParamsProperty = DependencyProperty.Register(nameof(ExtraParams), typeof(NoteExtraParams), typeof(Note),
            new PropertyMetadata(null, OnExtraParamsChanged));

        public static readonly Comparison<Note> TimingComparison = (x, y) => {
            if (x == null) {
                throw new ArgumentNullException(nameof(x));
            }
            if (y == null) {
                throw new ArgumentNullException(nameof(y));
            }
            if (x.Equals(y)) {
                return 0;
            }
            if (x.Bar != y.Bar) {
                return x.Bar.Index.CompareTo(y.Bar.Index);
            }
            var r = x.IndexInGrid.CompareTo(y.IndexInGrid);
            if (r == 0 && x.Type != y.Type && (x.Type == NoteType.VariantBpm || y.Type == NoteType.VariantBpm)) {
                // The Variant BPM note is always placed at the end on the same grid line.
                return x.Type == NoteType.VariantBpm ? 1 : -1;
            } else {
                return r;
            }
        };

        public int CompareTo(Note other) {
            if (other == null) {
                throw new ArgumentNullException(nameof(other));
            }
            if (Equals(other)) {
                return 0;
            } else {
                return TimingComparison(this, other);
            }
        }

        public static readonly Comparison<Note> TrackPositionComparison = (n1, n2) => ((int)n1.FinishPosition).CompareTo((int)n2.FinishPosition);

        public static bool operator >(Note left, Note right) {
            return TimingComparison(left, right) > 0;
        }

        public static bool operator <(Note left, Note right) {
            return TimingComparison(left, right) < 0;
        }

        public static bool IsTypeGaming(NoteType type) {
            return type == NoteType.TapOrFlick || type == NoteType.Hold;
        }

        public static bool IsTypeSpecial(NoteType type) {
            return type == NoteType.VariantBpm;
        }

        public static void ConnectSync(Note n1, Note n2) {
            if (n1 != null) {
                n1.SyncTarget = n2;
            }
            if (n2 != null) {
                n2.SyncTarget = n1;
            }
        }

        public static void ConnectFlick(Note first, Note second) {
            if (first != null) {
                first.NextFlickNote = second;
            }
            if (second != null) {
                second.PrevFlickNote = first;
            }
        }

        public static void ConnectHold(Note n1, Note n2) {
            if (n1 != null) {
                n1.HoldTarget = n2;
            }
            if (n2 != null) {
                n2.HoldTarget = n1;
            }
        }

        internal bool TryGetFlickGroupID(out FlickGroupModificationResult modificationResult, out int knownGroupID, out Note groupStart) {
            if (!IsFlick || (IsHoldEnd && !HasNextFlick)) {
                knownGroupID = EntityID.Invalid;
                modificationResult = FlickGroupModificationResult.Declined;
                groupStart = null;
                return false;
            }
            var groupItemCount = 0;
            var temp = this;
            // Compiler trick.
            groupStart = temp;
            while (temp != null) {
                groupStart = temp;
                temp = temp.PrevFlickNote;
                ++groupItemCount;
            }
            temp = this;
            ++groupItemCount;
            while (temp.HasNextFlick) {
                temp = temp.NextFlickNote;
                ++groupItemCount;
            }
            if (groupItemCount < 2) {
                // Actually, the flick group is not fully filled. We should throw an exception.
                knownGroupID = EntityID.Invalid;
                modificationResult = FlickGroupModificationResult.Declined;
                return false;
            }
            if (groupStart.GroupID != EntityID.Invalid) {
                knownGroupID = groupStart.GroupID;
                modificationResult = FlickGroupModificationResult.Reused;
            } else {
                knownGroupID = EntityID.Invalid;
                modificationResult = FlickGroupModificationResult.CreationPending;
            }
            return true;
        }

        internal int GroupID { get; set; }

        [JsonConstructor]
        internal Note(int id, Bar bar) {
            ID = id;
            Bar = bar;
            IndexInGrid = 0;
            Type = NoteType.TapOrFlick;
            StartPosition = NotePosition.Nowhere;
            FinishPosition = NotePosition.Nowhere;
            FlickType = NoteFlickType.Tap;
        }

        internal void Reset() {
            if (SyncTarget != null) {
                SyncTarget.SyncTarget = null;
            }
            SyncTarget = null;
            if (NextFlickNote != null) {
                NextFlickNote.PrevFlickNote = null;
            }
            NextFlickNote = null;
            if (PrevFlickNote != null) {
                PrevFlickNote.NextFlickNote = null;
            }
            PrevFlickNote = null;
            if (HoldTarget != null) {
                HoldTarget.HoldTarget = null;
                if (HoldTarget != null) {
                    if (!HoldTarget.HasNextFlick && !HoldTarget.HasPrevFlick && HoldTarget.FlickType != NoteFlickType.Tap) {
                        HoldTarget.FlickType = NoteFlickType.Tap;
                    }
                }
            }
            FlickType = NoteFlickType.Tap;
            HoldTarget = null;
        }

        internal void SetSpecialType(NoteType type) {
            switch (type) {
                case NoteType.VariantBpm:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
            Type = type;
        }

        private static void OnTypeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            var note = (Note)obj;
            note.IsFlick = note.IsFlickInternal();
        }

        private static void OnFlickTypeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            var note = (Note)obj;
            note.IsFlick = note.IsFlickInternal();
        }

        private static void OnExtraParamsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            var note = (Note)obj;
            var oldParams = (NoteExtraParams)e.OldValue;
            var newParams = (NoteExtraParams)e.NewValue;
            if (oldParams != null) {
                oldParams.ParamsChanged -= note.ExtraParams_ParamsChanged;
            }
            if (newParams != null) {
                newParams.ParamsChanged += note.ExtraParams_ParamsChanged;
            }
        }

        private bool IsFlickInternal() {
            return Type == NoteType.TapOrFlick && (FlickType == NoteFlickType.FlickLeft || FlickType == NoteFlickType.FlickRight);
        }

        private void ExtraParams_ParamsChanged(object sender, EventArgs e) {
            ExtraParamsChanged.Raise(sender, e);
        }

        private Note _prevFlickNote;
        private Note _nextFlickNote;
        private Note _syncTarget;
        private Note _holdTarget;

    }
}
