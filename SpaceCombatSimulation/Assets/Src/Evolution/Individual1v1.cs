﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Src.Evolution
{
    public class Individual1v1 : BaseIndividual
    {
        public int Wins;
        public int Draws;
        public int Loses;

        public int MatchesPlayed { get { return Wins + Draws + Loses; } }

        public List<string> PreviousCombatants = new List<string>();

        private const int WIN_SCORE = 10;
        private const int DRAW_SCORE = -2;
        private const int LOOSE_SCORE = -10;

        public Individual1v1(string genome) : base(genome)
        {
        }

        public float AverageScore
        {
            get
            {
                if (MatchesPlayed > 0)
                {
                    return Score / MatchesPlayed;
                }
                else
                {
                    return 0;
                }
            }
        }

        public string PreviousCombatantsString {
            get
            {
                return string.Join(",", PreviousCombatants.Where(s => !string.IsNullOrEmpty(s)).ToArray());
            }
            set
            {
                PreviousCombatants = value.Split(',').Where(s => !string.IsNullOrEmpty(s)).ToList();
            }
        }

        public void RecordMatch(string otherCompetitor, string victor, float winScore, float losScore, float drawScore)
        {
            PreviousCombatants.Add(otherCompetitor);

            if (string.IsNullOrEmpty(victor))
            {
                Debug.Log("Draw");
                Draws++;
                Score += drawScore;
            }
            else
            {
                if (Summary.Genome == victor)
                {
                    Debug.Log(Summary.GetName() + " Wins!");
                    Wins++;
                    Score += winScore;
                }
                else if (victor == otherCompetitor)
                {
                    Loses++;
                    Score += losScore;
                }
                else
                {
                    Debug.LogWarning("Victor '" + victor + "' was not '" + Genome + "' or '" + otherCompetitor + "'");
                    Draws++;
                    Score += drawScore;
                }
            }
        }

        private static float ParsePart(string[] parts, int index)
        {
            float retVal = 0;
            if (parts.Length > index)
            {
                var intString = parts[index];
                float.TryParse(intString, out retVal);
            }
            return retVal;
        }

        public override string ToString()
        {
            var competitorsString = string.Join(",", PreviousCombatants.Where(s => !string.IsNullOrEmpty(s)).ToArray());
            var strings = new List<string>
                {
                    Genome,
                    Score.ToString(),
                     Wins.ToString(),
                    Draws.ToString(),
                    Loses.ToString(),
                   competitorsString.ToString()
                };

            return string.Join(";", strings.ToArray());
        }
    }
}
