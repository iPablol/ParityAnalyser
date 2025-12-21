
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

        internal static List<GameObject> renders = [];
        internal static List<Action> renderUpdaters = [];

        internal static Options options = new();

        [Init]
        private void Init()
        {
            SceneManager.sceneLoaded += SceneLoaded;
            _ui = new UI(this);
            options = Options.Load();
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

        public static void AddRender(GameObject renderer, Action updater)
        {
            renders.Add(renderer);
            renderUpdaters.Add(updater);
            atc.TimeChanged = Delegate.Combine(atc.TimeChanged, updater) as Action;
        }

        public static void ClearRenders()
        {
            foreach (var r in renders)
            {
                Object.DestroyImmediate(r);
            }
            renders.Clear();
            outline.ClearCache();

            foreach (var r in renderUpdaters)
            {
                atc.TimeChanged = Delegate.Remove(atc.TimeChanged, r) as Action;
            }

        }

        public void Analyse()
        {
            ClearRenders();
            Simulation sim = new Simulation(noteGrid.MapObjects.Where(x => x is BaseNote).ToList());
            sim.Run();

            RenderParities(sim.redParities, Color.red, options.renderLeftParityOutlines, options.renderLeftParitySabers);
            RenderParities(sim.blueParities, Color.blue, options.renderRightParityOutlines, options.renderRightParitySabers);

            if (options.animateLeftParities) AnimateParities(sim.redParities, Color.red);
            if (options.animateRightParities) AnimateParities(sim.blueParities, Color.blue);
        }

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
            Action timeChanged = () =>
            {
                float time = atc.CurrentJsonTime;
                Vector3 position = Interpolation.SamplePositionAtTime(parities, time);
                Quaternion rotation = Interpolation.SampleRotationAtTime(parities, time);
                Vector3 tip = position + (Saber.length * (rotation * Vector3.up));
                lr.SetPositions([position + time.Offset(), tip + time.Offset()]);

            };
            ParityAnalyser.AddRender(renderer, timeChanged);

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
            Action update = () =>
            {
                float time = atc.CurrentJsonTime;
                Vector3 position = Interpolation.SamplePositionAtTime(parities, time);
                DrawAngleIndicator(alr, position + time.Offset(), 0.2f, Interpolation.SampleWristAngleAtTime(parities, time));
            };
            ParityAnalyser.AddRender(angleRenderer, update);
        }

        internal void RenderParities(List<SaberSnapshot> parities, Color handColor, bool renderOutlines = true, bool renderSabers = true)
        {
            foreach (SaberSnapshot snap in parities)
            {
                if ((snap.note.Type != (int)NoteType.Bomb) || (snap.note.Type == (int)NoteType.Bomb && snap.reset))
                {
                    if (renderOutlines)
                    {

                        outline.AddToCache(snap.note, snap.parity == Parity.FOREHAND ? Color.white : Color.black);
                        
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
                    Action update = () => lr.SetPositions([snap.hilt + snap.note.Offset(), snap.tip + snap.note.Offset()]);
                    ParityAnalyser.AddRender(renderer, update);
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



        [Exit]
        private void Exit() { }
    }

    // If in a snapshot, represents the parity of that hit, if in the saber it represents the parity of the next hit
    public enum Parity
    {
        FOREHAND = -1,
        BACKHAND = 1
    }
}
