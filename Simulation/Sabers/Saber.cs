using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using Parity = ParityAnalyser.Parity;
using CutDir = ParityAnalyser.NoteDirection;
using Beatmap.Base;
using Beatmap.Enums;
using Unity.Collections;

using Hit = Intersections.IntersectionHit;
using BombCondition = System.Func<Beatmap.Base.BaseNote, bool>;
using static Beatmap.V4.V4CommonData;
using System.Collections;
using System.Net;

namespace ParityAnalyser.Sim
{
    public abstract class Saber
    {
        private Stack<SaberSnapshot> stack = [];

        public void PushToStack(BaseNote note) => stack.Push(TakeSnapshot(note));

        public BaseNote PopStack()
        {
            SaberSnapshot snap = stack.Pop();
            transform.position = snap.position;
            transform.rotation = snap.rotation;
            parity = snap.parity;
            wristAngle = snap.wristAngle;
            hasReset = snap.reset;
            return snap.note;
        }

        public void ClearStack() => stack.Clear();

        public SaberSnapshot TakeSnapshot(BaseNote note) => new(note, transform.position, transform.rotation, parity, wristAngle, hasReset);
        public List<ISimulationObject> notes { get; private set; } = [];
        public List<BombGroup> bombGroups { get; private set; } = [];
        public Saber(List<BaseNote> relevantNotes, Parity start = Parity.FOREHAND)
        {
            this.transform.rotation = Quaternion.identity;
            this.parity = start;
            

            List<ISimulationObject> sliderGroups = ExtractSliderGroups(relevantNotes).ToList();
            for (int i = 0; i < sliderGroups.Count; i++)
            {
                SliderGroup group = (SliderGroup)sliderGroups[i];
                if (group.isStack && !group.isDotStack)
                {
                    sliderGroups[i] = group.Order(group.GetOrder());
                }
            }

            // Replace sliders
            List<ISimulationObject> currentNotes = (from simObject in
                                                (from note in relevantNotes
                                                   where !(sliderGroups.Cast<SliderGroup>().Any(slider => slider.Notes().Contains(note))) 
                                                   select new Note(note) as ISimulationObject).Concat(sliderGroups)
                                             orderby simObject.Time() ascending
                                             select simObject).ToList();

            List<ISimulationObject> bombGroups = ExtractBombGroups(currentNotes).ToList();
            
            // Remove bombs
            currentNotes = (from note in currentNotes
                     where note is SliderGroup || (note is Note n && !n.Value.IsBomb())
                     select note).ToList();



            this.notes = (from simObject in currentNotes.Concat(bombGroups)
                          orderby simObject.Time() ascending
                          select simObject).ToList();

            
            List<ISimulationObject> merged = [];
            for (int i = 0; i < this.notes.Count - 1; i++)
            {
                // 2 consecutive bomb groups
                if (this.notes[i] is BombGroup group1 &&
                    this.notes[i + 1] is BombGroup group2)
                {
                    var mergedGroup = group1.Merge(group2);

                    this.notes[i] = mergedGroup;

                    
                    merged.Add(group2);
                }
                // all of the bombgroup is in the same beat as the next one
                else if (i < this.notes.Count - 2 && this.notes[i] is BombGroup g1 && this.notes[i + 2] is BombGroup g2 && g1.maxTime.NearlyEqualTo(g2.Time(), 1 / 8f) && g1.minTime.NearlyEqualTo(g2.Time(), 1 / 8f))
                {
                    var mergedGroup = g1.Merge(g2);

                    this.notes[i + 2] = mergedGroup;

                    merged.Add(g1);
                }
                // all of the bombgroup is in the same beat as the previous one (MARENOL beat 52)
                else if (i > 1 && this.notes[i] is BombGroup shortGroup && this.notes[i - 2] is BombGroup longGroup && shortGroup.maxTime.NearlyEqualTo(longGroup.nextObject.Time(), 1 / 8f) && shortGroup.minTime.NearlyEqualTo(longGroup.nextObject.Time(), 1 / 8f))
                {
                    var mergedGroup = shortGroup.MergeToPrevious(longGroup);

                    this.notes[i - 2] = mergedGroup;

                    merged.Add(longGroup);
                    merged.Add(shortGroup);
                }
            }

            this.notes = this.notes.Where(o => !merged.Contains(o)).ToList();

            this.bombGroups = this.notes.Where(o => o is BombGroup).Cast<BombGroup>().ToList();
            bool debug = (this is LeftSaber && ParityAnalyser.options.debugLeftBombCollisions) ||
                        (this is RightSaber && ParityAnalyser.options.debugRightBombCollisions);
            if (debug)
            {
                foreach (var group in this.bombGroups)
                {
                    //Debug.Log("");
                    //Debug.Log("Group " + group.Time());
                    //List<BombCluster> clusters = group.GetClusters().ToList();
                    //foreach (var cluster in clusters)
                    //{
                    //    Debug.Log(cluster.GetHitbox().Count());
                    //    cluster.Render();
                    //}
                    //Debug.Log("");
                }
            }

            this.wristAngle = 0;
        }
        private GameObject dummyObject = new GameObject("saber dummy object");
        protected Transform transform => dummyObject.transform;
        public Parity parity 
        {  
            get; 
            protected set; 
        }

        private bool hasReset = false;

        public virtual IEnumerable<SaberSnapshot> FirstSwing()
        {
            yield return TakeSnapshot(new Note(new BaseNote() { JsonTime = 0 }));
            foreach (var snapshot in this.Swing(null, notes.First()))
            {
                yield return snapshot;
            }
        }


        public virtual IEnumerable<SaberSnapshot> Swing(ISimulationObject previousObject, ISimulationObject nextObject) 
        {
            //Debug.Log(nextObject.Time() + " " + parity.ToString());

			hasReset = false;
            //Debug.Log($"Prev: {previousObject?.GetType()}, Next: {nextObject?.GetType()}");
            //Debug.Log("");

            if (nextObject is BombGroup group)
            {
                //Debug.Log(group.ToString());
                foreach (var reset in HandleBombGroup(group)) yield return reset;
            }
            else if (nextObject is SliderGroup slider)
            {
                
                //Debug.Log($"Slider at: {nextObject.Time()} with {slider.noteCount} notes");
                if (slider.isDotStack)
                {
                    slider = slider.OrderFullDotStack(previousObject, this);
                }
                foreach ((float angle, BaseNote nextNote) in slider.GetAngles(this))
                {
                    MoveTowardsNote(angle, nextNote);
                    yield return TakeSnapshot(nextNote);
                }
                
                this.parity = this.parity.Other();
                
            }
            else
            {
                RotateTowards(previousObject, nextObject);
                yield return TakeSnapshot(nextObject.FirstNote());
                
                    this.parity = this.parity.Other();
                
            }
            
        }

        // wristAngle could maybe be a boolean to keep the current angle
        public virtual SaberSnapshot Reset(BaseNote culprit, Parity parity, string reason, float wristAngle = 0f) 
        {
            //ParityAnalyser.outline.AddToCache(culprit, Color.yellow);
            Debug.Log($"Reset at beat {culprit.JsonTime}. Reason: {reason}");
            hasReset = true;
            this.parity = parity;
            this.transform.rotation = Quaternion.identity;
            transform.localRotation *= Quaternion.AngleAxis(parity.Bool() ? 0f : 180f, Vector3.right);
            this.wristAngle = wristAngle;

            return TakeSnapshot(culprit);
        }

        protected void MoveTo(Vector2 pos) { this.transform.position = new Vector3(Mathf.Clamp(pos.x, Simulation.minSaberX, Simulation.maxSaberX), 
                                                                        Mathf.Clamp(pos.y, Simulation.minSaberY, Simulation.maxSaberY), 
                                                                        transform.position.z); }
        protected void MoveTo(Vector3 pos) => MoveTo((Vector2)pos);

        protected virtual void RotateTowards(ISimulationObject previousObject, ISimulationObject nextObject)
        {
            BaseNote previousNote = previousObject is BombGroup group ? group.startNote : previousObject?.LastNote() ?? null;
            BaseNote nextNote = nextObject.FirstNote();
            float desiredAngle = CutAngle(previousNote, nextNote);
            MoveTowardsNote(desiredAngle, nextNote);

        }

        protected virtual void MoveTowardsNote(float angle, BaseNote nextNote)
        {

            // IMPORTANT: check Howl in the night sky ending (it causes resets)
            /*
             * should somehow increase the range of desired angles?
             * get this back up when working on old maps
             */
            float roll = angle - wristAngle;
            //if (roll < -315 || roll > 315)
            //{
            //    Reset(nextNote, this.parity.Other(), "Wristroll too large");
            //    return;
            //}
            this.wristAngle = angle;
            transform.rotation = Quaternion.AngleAxis(wristAngle, Vector3.forward);
            transform.localRotation *= Quaternion.AngleAxis(parity.Bool() ? -180f : 0f, Vector3.right);
            
            if ((CutDir)nextNote.CutDirection != CutDir.ANY)
            {
                MoveTo((Vector3)nextNote.Position() - cutOffset);
            }
            else
            {
                MoveTo(nextNote.Position());
            }
        }

        public virtual float CutAngle(BaseNote prevNote, BaseNote nextNote, bool moveToDot = true, bool isSlider = false)
        {
            // IMPORTANT: check Kyuukou dot at 160 (causes reset when using max 315 roll)
            float desiredAngle = DesiredAngle((NoteDirection)nextNote.CutDirection);
            if (prevNote != null)
            {
                bool shouldKeepAngle = prevNote.IsInlineWith(nextNote) || nextNote.IsInvert(prevNote, wristAngle + parityAngle) || nextNote.PosY == prevNote.PosY;
                if (shouldKeepAngle && ParityAnalyser.options.debugDots && nextNote.IsDot())
                {
                    ParityAnalyser.outline.AddToCache(nextNote, Color.cyan);
                }
                if ((nextNote.IsDot() && !shouldKeepAngle) || isSlider)
                {
                    // TODO: maybe check inlines (example: abstruse dilemma) and inverts (example: Bad apple (Bitz) )
                    if (moveToDot || isSlider)
                        MoveTo(prevNote.Position());
                    Vector2 dir = (nextNote.Position() - (Vector2)transform.position).normalized;
                    if (!parity.Bool()) dir = -dir;
                    float signedAngle = Vector2.SignedAngle(Vector2.down, dir);
                    desiredAngle = Mathf.DeltaAngle(0f, signedAngle);
                    //Debug.Log($"Beat: {nextNote.JsonTime}, Angle: {desiredAngle}, Signed: {signedAngle}");

                }
            }
            return desiredAngle;
        }

        protected virtual float WristRoll(BaseNote prevNote, ISimulationObject nextObject, bool moveToDot = true)
        {
            float angle = 0f;
            if (nextObject is SliderGroup slider)
            {
                if (slider.isDotStack)
                {
                    slider = slider.OrderFullDotStack(new Note(prevNote), this);
                }
                angle = slider.GetAngles(this, !moveToDot).First().Item1;
            }
            else
            {
                angle = CutAngle(prevNote, nextObject.FirstNote(), moveToDot);
            }

            return Mathf.DeltaAngle(wristAngle, angle);
        }

        private IEnumerable<SaberSnapshot> HandleBombGroup(BombGroup group)
        {

            if (group.endNote == null)
            {
                // Map ends with bombs
                yield break;
            }

            // First, try to resolve with exploration
            bool exploredSuccesfully = false;
            foreach (SaberSnapshot reset in ExploreBombGroup(group, group.startNote.JsonTime, group.endNote.JsonTime))
            {
                yield return reset;
                exploredSuccesfully = true;
            }
            if (exploredSuccesfully)
            {
                yield break;
            }
            // Otherwise try common resets

            // TODO?: resets marked by bombs to the sides of the note

            float roll = WristRoll(group.startNote, group.nextObject, false);

            List<BombCondition> bottomRowReset = [
                (note) => note.LeftOuterLane() && note.BottomRow(),
                (note) => note.LeftInnerLane() && note.BottomRow(),
                (note) => note.RightInnerLane() && note.BottomRow(),
                (note) => note.RightOuterLane() && note.BottomRow(),
                ];
            bool isBottomRowReset = group.Satisfy(bottomRowReset).Count() >= 3 /*group.Satisfies(bottomRowReset)*/;
            
            Debug.Log($"Beat: {group.Time()}, Wrist: {wristAngle}, roll: {roll}, comfortable: {RollsComfortably(roll)}");

            bool shouldResetDueToAngle = Mathf.Abs(wristAngle + roll) > 90f && (!RollsComfortably(roll) || Mathf.Abs(roll) >= 135f || Mathf.Abs(wristAngle + roll) >= 180f);
            if (shouldResetDueToAngle && /*don't reset for dots that cause very little roll*/ Mathf.Abs(roll) > 30f)
            {
                // MARENOL beat 58: left saber rolls clockwise, conflicts with B.B.K.K.B.K.K
                yield return Reset(group.bombs[0], parity.Other(), "Wrist roll caused too much wrist angle (bombs)");
                yield break;
            }
            bool shouldResetDueToRoll = Mathf.Abs(roll) >= 180f;

            
            if ((!group.Any(bomb => bomb.MiddleRow() || bomb.TopRow())) && parity == Parity.BACKHAND && Mathf.Abs(wristAngle) <= 90f && isBottomRowReset && (shouldResetDueToAngle || shouldResetDueToRoll))
            {
                Debug.Log($"Angle: {shouldResetDueToAngle} - wa: {wristAngle} - r: {roll} - c: {RollsComfortably(roll)}\n" +
                    $"Roll: {shouldResetDueToRoll} - r: {roll}");
                yield return Reset(group.bombs[0], Parity.FOREHAND, "Bottom row reset");
                yield break;
            }

            List<BombCondition> topRowReset = [
                (note) => note.LeftOuterLane() && note.TopRow(),
                (note) => note.LeftInnerLane() && note.TopRow(),
                (note) => note.RightInnerLane() && note.TopRow(),
                (note) => note.RightOuterLane() && note.TopRow(),
                ];
            //bool isTopRowReset = group.Satisfy(topRowReset).Count() >= 3;
            if ((!group.Any(bomb => bomb.MiddleRow() || bomb.BottomRow())) && parity == Parity.FOREHAND && Mathf.Abs(wristAngle) <= 90f && group.Satisfies(topRowReset) && (shouldResetDueToAngle || shouldResetDueToRoll))
            {
                yield return Reset(group.bombs[0], Parity.BACKHAND, "Top row reset");
                yield break;
            }

            // Quick bomb reset
            float quickBombThreshold = 1 / 4f;
            IEnumerable<BaseNote> inlineBombs = group.Where(bomb => bomb.PosX == group.startNote.PosX && bomb.PosY == group.startNote.PosY);
            if (inlineBombs.Count() > 0)
            {
                bool quickInlineBomb = inlineBombs.OrderByDescending(bomb => bomb.JsonTime).Last().JsonTime - group.startNote.JsonTime < quickBombThreshold;

                if (quickInlineBomb)
                {
                    yield return Reset(group.bombs[0], parity.Other(), "Inline bomb");
                    yield break;
                }
            }


        }

        protected virtual IEnumerable<SaberSnapshot> ExploreBombGroup(BombGroup group, float startTime, float endTime)
        {
            bool debug = (this is LeftSaber && ParityAnalyser.options.debugLeftBombCollisions) ||
                        (this is RightSaber && ParityAnalyser.options.debugRightBombCollisions);
            // Simulate hit momentum if above middle row (bottom row hits are more controlled)
            if (transform.position.y > 1)
                MoveTo(transform.position + transform.up);

            bool hasRolledAway = false;
            bool isFirstCluster = true;
            foreach ((BombCluster cluster1, BombCluster cluster2) in group.GetClusterPairs(ParityAnalyser.options.bombClusterMerging))
            {
                DodgeBombs(group, cluster1, debug);
                Start:
                float roll = WristRoll(group.startNote, group.nextObject, false);
                float swingOffset = parityAngle;
                float startAngle = wristAngle + swingOffset;
                // Sound chimera beat 609: left saber is forced to have an upright angle but next note is diagonal (can be fixed by turning cluster merging off)
                float endAngle =
                    (hasReset ? DesiredAngle((CutDir)group.endNote.CutDirection) + swingOffset :
                    wristAngle + roll + swingOffset);

                // Wrist angle exceeds natural rotation limits or why would you reset to then rotate your wrist past 90 degrees (second condition was causing an infinite loop)
                float nextAngle = endAngle - swingOffset;
                // Da mama 4-wide stacks roll to the other side
                bool unnatural = (nextAngle > maxCCAngle || nextAngle < maxClockwiseAngle) || (hasReset && Math.Abs(nextAngle) > 90f);
                //Debug.Log($"Freeze: {freeze}     {startAngle} - {endAngle}");
                if (!group.singleBeat && unnatural && nextAngle != wristAngle && !hasReset)
                {
                    yield return Reset(cluster1.aBomb, parity.Other(), "Unnatural wrist roll (bomb exploration) " + $"{wristAngle} - {endAngle - swingOffset}", wristAngle);
                    goto Start;
                }
                float lerpFactor = (endTime - startTime);

                float cluster1LerpAmount = (cluster1.time - startTime) / lerpFactor;
                float cluster2LerpAmount = (cluster2.time - startTime) / lerpFactor;
                if (debug)
                    cluster1.Render();

                // TODO: fix bombsage single bomb bullshit (example: MARENOL at the beginning)
                foreach (OrientedRect hitbox in cluster1.GetHitbox())
                {
                    if (group.singleBeat)
                    {
                        // Collision detection is capped at 180 degrees so if we find a higher roll we should interpolate multiple times in the same cluster
                        bool intersects = Collision.SwingPathIntersects(hilt, startAngle, wristAngle + roll + swingOffset, hitbox, debug, cluster1.aBomb);
                        if (intersects)
                        {
                            yield return Reset(cluster1.aBomb, parity.Other(), "Bomb projection intersects");
                            break;
                        }
                    }
                    else if (isFirstCluster)
                    {
                        bool intersects = Collision.SwingPathIntersects(hilt, startAngle, Mathf.Lerp(startAngle, endAngle, cluster2LerpAmount), hitbox, debug, cluster1.aBomb);
                        if (intersects)
                        {
                            yield return Reset(cluster1.aBomb, parity.Other(), "Bomb projection intersects");
                            break;
                        }
                    }
                    else
                    { 
                        bool swingPathCollides = Collision.SwingPathIntersects(hilt, Mathf.Lerp(startAngle, endAngle, cluster1LerpAmount), Mathf.Lerp(startAngle, endAngle, cluster2LerpAmount), hitbox, debug, cluster1.aBomb);

                        if (swingPathCollides)
                        {
                            if (!hasRolledAway && group.endNote.IsDot() && Mathf.Abs(roll) < 30f)
                            {
                                // Roll to preferred direction or resting position?
                                // TODO: angle selection logic (pink diamond beat 213)
                                wristAngle += -Mathf.Sign(wristAngle) * 45f;
                                hasRolledAway = true;
                                goto Start;
                            }
                            startTime = cluster1.time;
                            yield return Reset(cluster1.aBomb, parity.Other(), "Swing path", wristAngle);
                            break;
                        }
                    }
                }
                isFirstCluster = false;
                // TODO: consider order of dodge and check collision (dodging first is more realistic in the sense that it causes less collision resets but it breaks the diagonal bottom row resets)
                DodgeBombs(group, cluster1, debug);
            }
            
            yield break;
        }

        private void DodgeBombs(BombGroup group, BombCluster currentCluster, bool debug = false)
        {
            Vector2 hiltPos = new Vector2(hilt.x, hilt.y);

            Vector2 totalForce = Vector2.zero;

            int count = 0;
            float countThreshold = 0.2f;
            foreach (BaseNote bomb in group.After(currentCluster.time))
            {
                float effectRadius = 0.6f;
                Vector2 bombPos = bomb.BombDodgeCenter();
                float timeDistance = bomb.JsonTime - currentCluster.time;
                float projectionDistance = Vector2.Distance(bombPos, hiltPos);

                Vector2 dir = (hiltPos - bombPos).normalized;

                float globalFactor = 2f;
                float timeScaleFactor = 0.87f, distanceScaleFactor = 1f;

                float timeSpread = 0.3f, timeShift = 0.15f;
                float timeMagnitude = Mathf.Exp(-1f * Mathf.Pow((timeDistance - timeShift) / timeSpread, 2));

                float distanceMagnitude = Mathf.Exp(-projectionDistance * projectionDistance) / effectRadius;

                float magnitude = globalFactor * (timeScaleFactor * timeMagnitude * distanceScaleFactor * distanceMagnitude);

                totalForce += dir * magnitude;
                if (magnitude >= countThreshold) count++;
            }
            if (count == 0) count++;
            totalForce /= count;
            //totalForce += RestForce();

            // TODO: tweak force (example: MARENOL beat 42 and da mama drop)
            Vector2 dirToNote = (group.endNote.Position() - hiltPos).normalized;
            float timeDistanceToNote = group.endNote.JsonTime - currentCluster.time,
                distanceToNote = Vector2.Distance(group.endNote.Position(), hiltPos),
                distanceDamp = 4f / (group.endNote.JsonTime - group.startNote.JsonTime), // Move more for longer bomb groups and viceversa
                noteForce = Mathf.Clamp(-Mathf.Log(timeDistanceToNote) * (distanceToNote / distanceDamp), 0, distanceToNote);
            totalForce += dirToNote * noteForce;

            MoveTo(transform.position + (Vector3)totalForce);
            if (debug)
            {
                Utils.RenderLine((Vector3)hiltPos, transform.position, Color.yellow, Color.black, sync: currentCluster.aBomb);
            }
            if ((this is LeftSaber && ParityAnalyser.options.renderLeftBombDodgePoints) ||
                (this is RightSaber && ParityAnalyser.options.renderRightBombDodgePoints))
            {
                foreach (BaseNote bomb in group.bombs)
                {
                    Utils.RenderSphere(bomb.BombDodgeCenter(), 0.2f, Color.red, bomb);
                }

            }
            //Debug.Log(transform.position);
        }

        public virtual IEnumerable<ISimulationObject> ExtractBombGroups(List<ISimulationObject> notes)
        {
            // TODO: maybe make a bomb group if map starts with bombs
            bool firstNoteHappened = false;
            for (int i = 0; i < notes.Count; i++)
            {
                if (notes[i] is SliderGroup || (notes[i] is Note n && !n.Value.IsBomb())) firstNoteHappened = true;
                if (firstNoteHappened && notes[i] is Note note && note.Value.IsBomb())
                {
                    ISimulationObject start = notes[i - 1];
                    List<Note> bombs = [];
                    while (notes[i] is Note nextNote && nextNote.Value.IsBomb())
                    {
                        bombs.Add(nextNote);
                        if (i == notes.Count - 1)
                        {
                            yield return new BombGroup(start, bombs, null);
                            yield break;
                        }
                        i++;
                    }
                    yield return new BombGroup(start, bombs, notes[i]);
                }
            }

        }

        public virtual IEnumerable<ISimulationObject> ExtractSliderGroups(List<BaseNote> allNotes)
        {
            allNotes = allNotes.Where(note => !note.IsBomb()).ToList();
            for (int i = 1; i < allNotes.Count; i++)
            {
                if (Utils.IsStackOrSlider(allNotes[i - 1], allNotes[i]))
                {
                    BaseNote previousNote = i <= 1 ? null : allNotes[i - 2];
                    List<BaseNote> slider = [allNotes[i - 1]];
                    while (Utils.IsStackOrSlider(allNotes[i - 1], allNotes[i]))
                    {
                        slider.Add(allNotes[i]);
                        i++;
                        if (i >= allNotes.Count) break;
                    }
                    yield return new SliderGroup(previousNote, slider);
                }
            }
        }

        public void RenderBombGroups(bool renderStart = false, bool renderEnd = false)
        {
            foreach (BombGroup group in bombGroups)
            {
                Color groupColor = Utils.RandomColor();
                if (renderStart) Utils.RenderSphere(new Vector3(group.startNote.PosX, 3, 0f), 0.5f, groupColor, group.startNote);
                foreach (BaseNote bomb in group.bombs)
                {
                    ParityAnalyser.outline.AddToCache(bomb, groupColor);
                }
                if (renderEnd && group.endNote != null) Utils.RenderSphere(new Vector3(group.endNote.PosX, 4, 0f), 0.3f, groupColor, group.endNote);
            }
        }

        protected abstract Vector2 restPoint { get; }
        protected Vector2 RestForce()
        {
            Vector2 dir = (restPoint - (Vector2)transform.position).normalized;
            float radius = 1.5f;
            float scaleFactor = 1f, power = 1f;
            float magnitude = Mathf.Min(scaleFactor * Mathf.Max(0, Mathf.Pow(Vector2.Distance(restPoint, transform.position) - radius, power)), 4f);
            return dir * magnitude;
        }

        public List<SliderGroup> sliderGroups { get; private set; } = [];

        private Vector3 cutOffset => transform.up * CutDistance;
        public static readonly float CutDistance = 0.5f;

        protected abstract float maxClockwiseAngle { get; }
        protected abstract float maxCCAngle { get; }
        protected abstract float preferredRollDirection { get; }

        public bool RollsComfortably(float roll) => (wristAngle != 0f && preferredRollDirection == Mathf.Sign(roll))
                                                || (wristAngle == 0f && roll == 0f);

        public float wristAngle { get; protected set; }

        public float parityAngle => (parity.Bool() ? 180f : 0f);

        public static readonly float length = 2.1f;
        public Vector3 hilt => this.transform.position;
        public Vector3 tip => this.transform.position + (length * transform.up);

        public IEnumerator<ISimulationObject> GetEnumerator() => this.notes.GetEnumerator();
        public IEnumerable<(ISimulationObject, ISimulationObject)> GetPairs() => new OverlappingPairIterator<ISimulationObject>(this.notes, false);

        public static implicit operator List<ISimulationObject>(Saber saber) => saber.notes;

        protected abstract float DesiredAngle(CutDir dir);
    }
}
