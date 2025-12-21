using Beatmap.Base;
using Beatmap.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Parity = ParityAnalyser.Parity;

namespace ParityAnalyser
{

    // Pink diamond: beat 134 full dot stack direction should go down (check when fixed bombs pushing saber into other bombs)
    public record struct SliderGroup(BaseNote previousNote, List<BaseNote> slider) : ISimulationObject
    {
        public bool isFirstSwing => previousNote == null;

        public bool isStack => (from note in slider
                                group note by note.JsonTime into g
                                select g.Key).Count() <= 1;

        public bool isFullyHorizontal => (from note in slider
                                            group note by note.PosY into g
                                            select g.Key).Count() <= 1;

        public bool isDotStack => isStack && slider.All(note => note.CutDirection == (int)NoteDirection.ANY);

        public float Time() => slider.First().JsonTime;
        public BaseNote FirstNote() => slider.First();
        public BaseNote LastNote() => slider.Last();

        public IEnumerable<BaseNote> Notes() => slider;
        public IEnumerator<BaseNote> GetEnumerator() => slider.GetEnumerator();

        public int noteCount => slider.Count();

        /* If stack is all dots
         *  if it's fully horizontal first note is closest horizontally and angle is horizontal
         *  
         *  otherwise first note is closest vertically
         *  
         *  select closest angle to wrist angle
         * */

        public void OrderFullDotStack(Vector2 saberPos, float wristAngle, Parity parity)
        {
            if (isFullyHorizontal)
            {
                BaseNote firstNote = (from note in slider
                                      orderby Math.Abs(note.PosX - saberPos.x) ascending
                                      select note).First();
                float to90 = Mathf.Abs(Mathf.DeltaAngle(wristAngle, 90f));
                float toNeg90 = Mathf.Abs(Mathf.DeltaAngle(wristAngle, -90f));

                bool closerTo90 = to90 < toNeg90;
                Order(GetOrder(parity, closerTo90 ? -90f : 90f));
            }
            else
            {
                BaseNote firstNote = (from note in slider
                                      orderby Math.Abs(note.PosY - saberPos.y) ascending
                                      select note).First();
                float to180 = Mathf.Abs(Mathf.DeltaAngle(wristAngle, 180f));
                float to0 = Mathf.Abs(Mathf.DeltaAngle(wristAngle, 0));
                float toNeg180 = Mathf.Abs(Mathf.DeltaAngle(wristAngle, -180f));

                float closer180 = Mathf.Min(Mathf.Abs(to180), Mathf.Abs(toNeg180));
                bool closerTo180 = closer180 < to0;

                Order(GetOrder(parity, closerTo180 ? 180f : 0f));
            }
        }

        private float GetAngle(Parity parity)
        {
            bool horizontal = slider.Any(note => note.CutDirection == (int)NoteDirection.LEFT ||  note.CutDirection == (int)NoteDirection.RIGHT);
            if (horizontal)
            {
                bool right = slider.Any(note => note.CutDirection == (int)NoteDirection.RIGHT);
                if (right)
                {
                    return parity.Bool() ? -90f : 90f;
                }
                else
                {
                    return parity.Bool() ? 90f : -90f;
                }
            }
            else
            {
                bool down = slider.Any(note => note.CutDirection == (int)NoteDirection.DOWN
                || note.CutDirection == (int)NoteDirection.DOWN_RIGHT || note.CutDirection == (int)NoteDirection.DOWN_LEFT);
                if (down)
                {
                    return parity.Bool() ? 0f : 180f;
                }
                else
                {
                    return parity.Bool() ? 180f : -0f;
                }
            }
        }

        public StackOrder GetOrder(Parity parity) => parity switch
        {
            Parity.FOREHAND => GetAngle(parity) switch
            {
                >= -45f and <= 45f => StackOrder.TopToBottom,
                -90f => StackOrder.RightToLeft,
                90f => StackOrder.LeftToRight,
                <= -135f or >= 135f => StackOrder.BottomToTop,
                _ => StackOrder.TopToBottom
            },
            Parity.BACKHAND => GetAngle(parity) switch
            {
                >= -45f and <= 45f => StackOrder.BottomToTop,
                -90f => StackOrder.LeftToRight,
                90f => StackOrder.RightToLeft,
                <= -135f or >= 135f => StackOrder.TopToBottom,
                _ => StackOrder.TopToBottom
            }
        };

        public StackOrder GetOrder(Parity parity, float angle) => parity switch
        {
            Parity.FOREHAND => angle switch
            {
                >= -45f and <= 45f => StackOrder.TopToBottom,
                -90f => StackOrder.RightToLeft,
                90f => StackOrder.LeftToRight,
                <= -135f or >= 135f => StackOrder.BottomToTop,
                _ => StackOrder.TopToBottom
            },
            Parity.BACKHAND => angle switch
            {
                >= -45f and <= 45f => StackOrder.BottomToTop,
                -90f => StackOrder.LeftToRight,
                90f => StackOrder.RightToLeft,
                <= -135f or >= 135f => StackOrder.TopToBottom,
                _ => StackOrder.TopToBottom
            }
        };

        public void Order(StackOrder order)
        {
            switch (order)
            {
                case StackOrder.TopToBottom:
                    slider = slider.OrderByDescending(note => note.PosY).ToList();
                    break;
                case StackOrder.BottomToTop:
                    slider = slider.OrderByDescending(note => note.PosY).Reverse().ToList();
                    break;
                case StackOrder.LeftToRight:
                    slider = slider.OrderByDescending(note => note.PosX).ToList();
                    break;
                case StackOrder.RightToLeft:
                    slider = slider.OrderByDescending(note => note.PosX).Reverse().ToList();
                    break;
                default:
                    break;
            }
        }

    }

    public enum StackOrder
    {
        TopToBottom,
        BottomToTop,
        LeftToRight,
        RightToLeft
    }
}
