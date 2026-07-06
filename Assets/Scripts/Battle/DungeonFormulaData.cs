using System;
using UnityEngine;

namespace Battle.Dungeon
{
    [Serializable]
    public struct DungeonFormulaData
    {
        [SerializeField] private DungeonFormulaType formula;
        [SerializeField] private double baseValue;
        [SerializeField] private double coefficient;
        [SerializeField] private double exponent;
        [SerializeField] private int stepInterval;
        [SerializeField] private double stepValue;
        [SerializeField] private DungeonRoundMode roundMode;

        public DungeonFormulaType Formula => formula;
        public double BaseValue => baseValue;
        public double Coefficient => coefficient;
        public double Exponent => exponent;
        public int StepInterval => stepInterval;
        public double StepValue => stepValue;
        public DungeonRoundMode RoundMode => roundMode;

        public double Evaluate(int currentStage, int formulaStartStage)
        {
            int localStage = Math.Max(0, currentStage - formulaStartStage);
            double value;

            switch (formula)
            {
                case DungeonFormulaType.Flat:
                    value = baseValue;
                    break;

                case DungeonFormulaType.Linear:
                    value = baseValue + coefficient * localStage;
                    break;

                case DungeonFormulaType.Power:
                    value = baseValue * Math.Pow(localStage + 1d, exponent);
                    break;

                case DungeonFormulaType.Exponential:
                    value = baseValue * Math.Pow(coefficient, localStage);
                    break;

                case DungeonFormulaType.Step:
                    int stepCount = stepInterval > 0
                        ? localStage / stepInterval
                        : 0;

                    value = baseValue + stepCount * stepValue;
                    break;

                default:
                    value = baseValue;
                    break;
            }

            return ApplyRound(value);
        }

        private double ApplyRound(double value)
        {
            switch (roundMode)
            {
                case DungeonRoundMode.Round:
                    return Math.Round(value, MidpointRounding.AwayFromZero);

                case DungeonRoundMode.Floor:
                    return Math.Floor(value);

                case DungeonRoundMode.Ceil:
                    return Math.Ceiling(value);

                default:
                    return value;
            }
        }
    }
}
