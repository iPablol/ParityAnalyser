using Beatmap.Base;
using Beatmap.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Parity = ParityAnalyser.Parity;

namespace ParityAnalyser.Sim
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

        public IEnumerable<(float, BaseNote)> GetAngles(Saber saber)
        {
            BaseNote firstNote = null, secondNote = null;
            for (int i = 0; i < noteCount; i++)
            {
                bool isLast = i == noteCount - 1;
                if (isLast)
                {
                    firstNote = Notes().ElementAt(i - 1);
                    secondNote = Notes().ElementAt(i);
                }
                else
                {
                    firstNote = Notes().ElementAt(i);
                    secondNote = Notes().ElementAt(i + 1);
                }
                BaseNote nextNote = isLast ? secondNote : firstNote;
                float angle = saber.CutAngle(firstNote, secondNote, true);
                if (angle == 180f && saber is RightSaber)
                {
                    angle = -180f;
                }
                yield return (angle, nextNote);
            }
        }

        public SliderGroup OrderFullDotStack(ISimulationObject lastObject, Saber saber)
        {
            float wristAngle = saber.wristAngle;
            Vector2 pos = saber.hilt;
            if (isFullyHorizontal)
            {
                BaseNote firstNote = (from note in slider
                                      orderby Math.Abs(note.PosX - pos.x) ascending
                                      select note).First();
                float to90 = Mathf.Abs(Mathf.DeltaAngle(wristAngle, 90f));
                float toNeg90 = Mathf.Abs(Mathf.DeltaAngle(wristAngle, -90f));
                bool closerTo90 = to90 < toNeg90;
                if (to90.NearlyEqualTo(toNeg90))
                {
                    bool left = !slider.Any(note => note.PosX > pos.x);
                    return Order(left ? StackOrder.RightToLeft : StackOrder.LeftToRight);
                }
                return Order(GetOrder(saber.parity, closerTo90 ? 90f : -90f));
            }
            else
            {
                BaseNote firstNote = (from note in slider
                                      orderby Math.Abs(note.PosY - pos.y) ascending
                                      select note).First();
                float to180 = Mathf.Abs(Mathf.DeltaAngle(wristAngle, 180f));
                float to0 = Mathf.Abs(Mathf.DeltaAngle(wristAngle, 0));
                float toNeg180 = Mathf.Abs(Mathf.DeltaAngle(wristAngle, -180f));

                float closer180 = Mathf.Min(Mathf.Abs(to180), Mathf.Abs(toNeg180));
                bool closerTo180 = closer180 < to0;
                bool bottom = slider.All(note => note.PosY <= pos.y);
                if (closer180.NearlyEqualTo(to0))
                {
                    if (pos.y.NearlyEqualTo(1f))
                    {
                        // prioritize down swings
                        return Order(StackOrder.TopToBottom);
                    }
                    return Order(bottom ? StackOrder.TopToBottom : StackOrder.BottomToTop);
                }
                if (closerTo180)
                {
                    SliderGroup topToBottom = Order(StackOrder.TopToBottom);
                    SliderGroup bottomToTop = Order(StackOrder.BottomToTop);

                    float roll1 = Mathf.Abs(wristAngle - topToBottom.GetAngles(saber).First().Item1);
                    float roll2 = Mathf.Abs(wristAngle - bottomToTop.GetAngles(saber).First().Item1);

                    return roll1 <= roll2 ? topToBottom : bottomToTop;
                    //return Order(Mathf.Abs(wristAngle) >= 90f ? StackOrder.TopToBottom : StackOrder.BottomToTop);
                }
                else
                {
                    return Order(bottom ? StackOrder.TopToBottom : GetOrder(saber.parity, wristAngle));
                }
            }
        }

        public StackOrder GetOrder()
        {
            bool horizontal = slider.Any(note => note.CutDirection == (int)NoteDirection.LEFT ||  note.CutDirection == (int)NoteDirection.RIGHT);
            if (horizontal)
            {
                bool right = slider.Any(note => note.CutDirection == (int)NoteDirection.RIGHT);
                if (right)
                {
                    return StackOrder.LeftToRight;
                }
                else
                {
                    return StackOrder.RightToLeft;
                }
            }
            else
            {
                bool down = slider.Any(note => note.CutDirection == (int)NoteDirection.DOWN
                || note.CutDirection == (int)NoteDirection.DOWN_RIGHT || note.CutDirection == (int)NoteDirection.DOWN_LEFT);
                if (down)
                {
                    return StackOrder.TopToBottom;
                }
                else
                {
                    return StackOrder.BottomToTop;
                }
            }
        }

        // Full dot stacks are resolved at simulation time
        public StackOrder GetOrder(Parity parity, float angle) => parity switch
        {
            Parity.FOREHAND => angle switch
            {
                > -67.5f and < 67.5f => StackOrder.TopToBottom,
                <= -67.5f and > -112.5f => StackOrder.RightToLeft,
                >= 67.5f and < 112.5f => StackOrder.LeftToRight,
                <= -112.5f or >= 112.5f => StackOrder.BottomToTop,
                _ => StackOrder.TopToBottom
            },
            Parity.BACKHAND => angle switch
            {
                > -67.5f and < 67.5f => StackOrder.BottomToTop,
                <= -67.5f and > -112.5f => StackOrder.LeftToRight,
                >= 67.5f and < 112.5f => StackOrder.RightToLeft,
                <= -112.5f or >= 112.5f => StackOrder.TopToBottom,
                _ => StackOrder.TopToBottom
            }
        };

        public SliderGroup Order(StackOrder order) => order switch
        {

            StackOrder.TopToBottom =>

            this with
            {
                slider = slider.OrderByDescending(note => note.PosY).ToList()

            },
            StackOrder.BottomToTop => this with
            {
                slider = slider.OrderByDescending(note => note.PosY).Reverse().ToList()
            },
            StackOrder.LeftToRight => this with
            {
                slider = slider.OrderByDescending(note => note.PosX).Reverse().ToList()
            },
            StackOrder.RightToLeft => this with
            {
                slider = slider.OrderByDescending(note => note.PosX).ToList()
            },
            _ => this
        };
    }
        

        

}

public enum StackOrder
{
    TopToBottom,
    BottomToTop,
    LeftToRight,
    RightToLeft
}

