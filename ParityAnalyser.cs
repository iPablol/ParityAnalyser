
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine;
using Beatmap.Base;
using System.Runtime.Remoting.Messaging;
using UnityEngine.Diagnostics;
using Beatmap.Enums;
using System.Data;
using System;

using Object = UnityEngine.Object;
using ParityAnalyser.Sim;
using Beatmap.Containers;
using Beatmap.Animations;

namespace ParityAnalyser
{
    [Plugin("ParityAnalyser")]
    public class ParityAnalyser
    {
        private UI _ui;
        static public BeatSaberSongContainer _beatSaberSongContainer;
        internal NoteGridContainer noteGrid;
        internal ObstacleGridContainer obstacleGrid;
        internal EventGridContainer eventGrid;
        internal ArcGridContainer arcGrid;

        internal TracksManager tracksManager;
        internal static AudioTimeSyncController atc;

        internal static Outline outline;

        [Init]
        private void Init()
        {
            SceneManager.sceneLoaded += SceneLoaded;
            _ui = new UI(this);

        }

        
        private void SceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (arg0.buildIndex == 3)
            {
                noteGrid = Object.FindObjectOfType<NoteGridContainer>();
                eventGrid = Object.FindObjectOfType<EventGridContainer>();
                obstacleGrid = Object.FindObjectOfType<ObstacleGridContainer>();
                arcGrid = Object.FindObjectOfType<ArcGridContainer>();
                tracksManager = Object.FindObjectOfType<TracksManager>();
                atc = Object.FindObjectOfType<AudioTimeSyncController>();

                outline = new Outline(this);
                MapEditorUI mapEditorUI = Object.FindObjectOfType<MapEditorUI>();
                _ui.AddMenu(mapEditorUI);
            }
        }

        public void Analyse()
        {
            Simulation sim = new Simulation(noteGrid.MapObjects.Where(x => x is BaseNote).ToList());
            sim.Run();

            RenderParities(sim.redParities, Color.red, true, true);
            RenderParities(sim.blueParities, Color.blue, true, true);
            AnimateParities(sim.blueParities, Color.blue);
            AnimateParities(sim.redParities, Color.red);
        }

        //      public void Analyse()
        //      {
        //          List<BaseNote> redNotes = noteGrid.MapObjects.Where(note => note.Type == (int)NoteType.Red).ToList();
        //          List<BaseNote> blueNotes = noteGrid.MapObjects.Where(note => note.Type == (int)NoteType.Blue).ToList();
        //          List<BaseNote> bombs = noteGrid.MapObjects.Where(note => note.Type == (int)NoteType.Bomb).ToList();

        //          RenderParities(AnalyseNotesParity(blueNotes, bombs));
        //	RenderParities(AnalyseNotesParity(redNotes, bombs));
        //}

        //     private List<(BaseNote, Parity)> AnalyseNotesParity(List<BaseNote> notes, List<BaseNote> bombs)
        //     {
        //List<Parity> parities = new List<Parity>();
        //int currentParity = -1;
        //         BaseNote prevNote = null;
        //         foreach ((BaseNote note1, BaseNote note2) in new Utils.OverlappingPairIterator<BaseNote>(notes, true))
        //         {
        //             parities.Add((Parity)currentParity);
        //             if (!(IsStackOrSlider(note1, note2) || IsReset(note1, note2, ref bombs, prevNote)))
        //             {
        //                 currentParity *= -1;
        //             }
        //             prevNote = note1;
        //         }
        //         return notes.Zip(parities, (a, b) => (a, b)).ToList();
        //     }

        //     private readonly float sliderThreshold = 1/16f;
        //     private bool IsStackOrSlider(BaseNote note1, BaseNote note2)
        //     {
        //         return note2.JsonTime - note1.JsonTime < sliderThreshold;
        //     }

        //     private bool IsReset(BaseNote note1, BaseNote note2, ref List<BaseNote> bombs, BaseNote prevNote) => IsBombReset(note1, note2, ref bombs, prevNote);

        //     private readonly float restingFactor = 10f;
        //     private readonly float rollThreshold = 120f;
        //     private bool IsBombReset(BaseNote note1, BaseNote note2, ref List<BaseNote> bombs, BaseNote prevNote)
        //     {
        //         bool intervalHasBomb = bombs.Any(bomb => note1.JsonTime < bomb.JsonTime && note2.JsonTime > bomb.JsonTime);
        //         if (intervalHasBomb)
        //         {
        //             // Resting factor should depend on BPM (time) and maybe count up to the bomb instead of the second note
        //             //float wristStartAngle = Mathf.Abs(note1.cutAngle(prevAngle));//Mathf.Lerp(note1.cutAngle(), 0, restingFactor * (note2.JsonTime - note1.JsonTime));
        //             //float wristEndAngle = Mathf.Abs(note2.cutAngle(note1.cutAngle(prevAngle)));
        //             //float wristRoll = Mathf.Abs(wristEndAngle - wristStartAngle);
        //             //if (wristRoll == 0) wristRoll = 360;
        //             //Debug.Log("Start Angle: " + wristStartAngle); Debug.Log("End Angle: " + wristEndAngle); Debug.Log("Roll: " + wristRoll); Debug.Log("Beat 1: " + note1.JsonTime); Debug.Log("Beat 2: " + note2.JsonTime); Debug.Log("");
        //             //if (wristRoll > rollThreshold) return true;

        //             Vector2 startDir = prevNote == null ? new Vector2(2f, 4f) /*first swing direction goes here*/ 
        //                 : Utils.CutVectorFromNoteToNote(prevNote, note1);
        //             Vector2 endDir = Utils.CutVectorFromNoteToNote(note1, note2);
        //             float roll = Vector2.Angle(startDir, endDir);
        //             Debug.Log("Beat 1: " + note1.JsonTime); Debug.Log("Beat 2: " + note2.JsonTime); Debug.Log("Start angle: " + startDir);
        //             Debug.Log("End angle: " + endDir); Debug.Log("Roll: " + roll);
        //             Debug.Log("");
        //             if (roll > rollThreshold)
        //             {
        //                 return true;
        //             }

        //         }
        //         return false;
        //     }

        internal void AnimateParities(List<SaberSnapshot> parities, Color handColor)
        {
            GameObject renderer = new GameObject("saber animation");
            LineRenderer lr = renderer.AddComponent<LineRenderer>();
            lr.positionCount = 2;

            Gradient g = new Gradient();
            g.SetKeys(
                new[] 
                {
                    new GradientColorKey(Color.black, 0f),
                    new GradientColorKey(handColor, 1f)
                },
                new[] 
                {
                    new GradientAlphaKey(1f, 1f),
                    new GradientAlphaKey(1f, 1f)
                }
            );

            lr.colorGradient = g;
            lr.startWidth = lr.endWidth = 0.05f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.material.color = handColor;

            GameObject angleRenderer = new GameObject("wrist angle animation");
            LineRenderer alr = angleRenderer.AddComponent<LineRenderer>();
            Gradient ag = new Gradient();
            ag.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.black, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 1f),
                    new GradientAlphaKey(1f, 1f)
                }
            );

            alr.colorGradient = ag;
            alr.material = new Material(Shader.Find("Sprites/Default"));
            alr.material.color = Color.white;
            alr.startWidth = alr.endWidth = 0.01f;

            atc.TimeChanged = (Action)Delegate.Combine(atc.TimeChanged, new Action(() =>
            {
                float time = atc.CurrentJsonTime;
                Vector3 position = Interpolation.SamplePositionAtTime(parities, time);
                Quaternion rotation = Interpolation.SampleRotationAtTime(parities, time);
                Vector3 tip = position + (Saber.length * (rotation * Vector3.up));
                lr.SetPositions([position + time.Offset(), tip + time.Offset()]);

                DrawAngleIndicator(alr, position + time.Offset(), 0.2f, Interpolation.SampleWristAngleAtTime(parities, time));
            }));
        }

        internal void RenderParities(List<SaberSnapshot> parities, Color handColor, bool renderOutlines = true, bool renderSabers = true)
        {
            foreach (SaberSnapshot snap in parities)
            {
                if (snap.isBombGroup) continue;
                if ((snap.note.Type != (int)NoteType.Bomb) || (snap.note.Type == (int)NoteType.Bomb && snap.reset))
                {
                    if (renderOutlines)
                    {
                        foreach (BaseNote note in snap.simObject.Notes())
                        {
                            outline.AddToCache(note, snap.parity == Parity.FOREHAND ? Color.white : Color.black);
                        }
                    }
                }
                if (renderSabers)
                {
                    GameObject renderer = new GameObject("line renderer");



                    LineRenderer lr = renderer.AddComponent<LineRenderer>();
                    lr.positionCount = 2;
                    lr.SetPositions([snap.hilt + snap.note.Offset(), snap.tip + snap.note.Offset()]);
                    Gradient g = new Gradient();
                    g.SetKeys(
                        new[] {
                        new GradientColorKey(Color.black, 0f),
                        new GradientColorKey(handColor, 1f)
                                        },
                                        new[] {
                        new GradientAlphaKey(1f, 1f),
                        new GradientAlphaKey(1f, 1f)
                                        }
                                    );

                    lr.colorGradient = g;
                    lr.startWidth = lr.endWidth = 0.05f;
                    lr.material = new Material(Shader.Find("Sprites/Default"));
                    lr.material.color = handColor;
                    atc.TimeChanged = (Action)Delegate.Combine(atc.TimeChanged, new Action(() => lr.SetPositions([snap.hilt + snap.note.Offset(), snap.tip + snap.note.Offset()])));
                }
            }
            outline.RefreshOutlines();
        }

        void DrawAngleIndicator(LineRenderer lr, Vector3 center, float radius, float angleDeg, float gapDeg = 10f, int segments = 64)
        {
            float halfGap = gapDeg * 0.5f;

            float startArc = angleDeg + halfGap;
            float endArc = angleDeg + 360f - halfGap;

            int arcSegments = Mathf.Max(2, Mathf.RoundToInt(
                segments * ((360f - gapDeg) / 360f)
            ));

            lr.positionCount = arcSegments + 3;

            int i = 0;

            // radial line 1
            lr.SetPosition(i++, center);

            // arc
            for (int s = 0; s <= arcSegments; s++)
            {
                float t = (float)s / arcSegments;
                float angle = Mathf.Lerp(startArc, endArc, t) * Mathf.Deg2Rad;

                Vector3 p = center + (new Vector3(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle),
                    0f
                ) * radius);

                lr.SetPosition(i++, p);
            }

            // radial line 2
            lr.SetPosition(i, center);
        }

        public enum Parity
        {
            FOREHAND = -1,
            BACKHAND = 1
        }


        [Exit]
        private void Exit() { }
    }
}
