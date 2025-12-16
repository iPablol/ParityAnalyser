
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine;
using Beatmap.Base;
using System.Runtime.Remoting.Messaging;
using UnityEngine.Diagnostics;
using Beatmap.Enums;
using System.Data;

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

                this.outline = new Outline(this);
                MapEditorUI mapEditorUI = Object.FindObjectOfType<MapEditorUI>();
                _ui.AddMenu(mapEditorUI);
            }
        }

        public void Analyse()
        {
            List<BaseNote> redNotes = noteGrid.MapObjects.Where(note => note.Type == (int)NoteType.Red).ToList();
            List<BaseNote> blueNotes = noteGrid.MapObjects.Where(note => note.Type == (int)NoteType.Blue).ToList();
            List<BaseNote> bombs = noteGrid.MapObjects.Where(note => note.Type == (int)NoteType.Bomb).ToList();

            RenderParities(AnalyseNotesParity(blueNotes, bombs));
			RenderParities(AnalyseNotesParity(redNotes, bombs));
		}

        private List<(BaseNote, Parity)> AnalyseNotesParity(List<BaseNote> notes, List<BaseNote> bombs)
        {
			List<Parity> parities = new List<Parity>();
			int currentParity = -1;
            foreach ((BaseNote note1, BaseNote note2) in new Utils.OverlappingPairIterator<BaseNote>(notes, true))
            {
                parities.Add((Parity)currentParity);
                if (!(IsStackOrSlider(note1, note2) || IsReset(note1, note2, ref bombs)))
                {
                    currentParity *= -1;
                }
            }
            return notes.Zip(parities, (a, b) => (a, b)).ToList();
        }

        private readonly float sliderThreshold = 1/16f;
        private bool IsStackOrSlider(BaseNote note1, BaseNote note2)
        {
            return note2.JsonTime - note1.JsonTime < sliderThreshold;
        }

        private bool IsReset(BaseNote note1, BaseNote note2, ref List<BaseNote> bombs) => IsBombReset(note1, note2, ref bombs);

        private readonly float restingFactor = 10f;
        private bool IsBombReset(BaseNote note1, BaseNote note2, ref List<BaseNote> bombs)
        {
            bool intervalHasBomb = bombs.Any(bomb => note1.JsonTime < bomb.JsonTime && note2.JsonTime > bomb.JsonTime);
            if (intervalHasBomb)
            {
                // Resting factor should depend on BPM (time) and maybe count up to the bomb instead of the second note
                float wristStartAngle = Mathf.Lerp(note1.cutAngle(), 0, restingFactor * (note2.JsonTime - note1.JsonTime));
                float wristRoll = 360 - Mathf.Abs(note2.cutAngle()) - Mathf.Abs(wristStartAngle);
                Debug.Log("Roll: " + wristRoll); Debug.Log("Beat 1: " + note1.JsonTime); Debug.Log("Beat 2: " + note2.JsonTime); Debug.Log("");
                if (wristRoll > 275) return true;
            }
            return false;
        }

        internal void RenderParities(List<(BaseNote, Parity)> parities)
        {
            foreach ((BaseNote note, Parity parity) in parities)
            {
                outline.AddToCache(note, parity == Parity.FOREHAND ? Color.green : Color.red);
            }
            outline.RefreshOutlines();
        }

        internal enum Parity
        {
            FOREHAND = -1,
            BACKHAND = 1
        }


        [Exit]
        private void Exit() { }
    }
}
