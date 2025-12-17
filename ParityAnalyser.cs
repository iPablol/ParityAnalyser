
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
        internal AudioTimeSyncController atc;

        private Outline outline;

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

                this.outline = new Outline(this);
                MapEditorUI mapEditorUI = Object.FindObjectOfType<MapEditorUI>();
                _ui.AddMenu(mapEditorUI);
            }
        }

        public void Analyse()
        {
            Simulation sim = new Simulation(noteGrid.MapObjects.Where(x => x is BaseNote).ToList());
            sim.Run();

            RenderParities(sim.redParities, Color.red);
            RenderParities(sim.blueParities, Color.blue);
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

        internal void RenderParities(List<SaberSnapshot> parities, Color handColor)
        {
            foreach (SaberSnapshot snap in parities)
            {
                if ((snap.note.Type != (int)NoteType.Bomb) || (snap.note.Type == (int)NoteType.Bomb && snap.reset))
                {
                    outline.AddToCache(snap.note, snap.reset ? Color.yellow : snap.parity == Parity.FOREHAND ? Color.green : Color.red);   
                }
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
            outline.RefreshOutlines();
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
