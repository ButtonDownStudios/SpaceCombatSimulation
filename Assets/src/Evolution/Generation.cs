﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.src.Evolution
{
    public class Generation
    {
        private System.Random _rng = new System.Random();
        private Dictionary<string, IndividualInGeneration> Individuals;

        public Generation()
        {
            //Debug.Log("Default Constructor");
            Individuals = new Dictionary<string, IndividualInGeneration>();
        }

        public Generation(string[] lines)
        {
            Individuals = lines.Select(l => new IndividualInGeneration(l)).ToDictionary(i => i.Genome, i => i);
        }

        public int CountIndividuals()
        {
            return Individuals.Count;
        }

        public bool AddGenome(string genome)
        {
            if (Individuals.ContainsKey(genome))
            {
                return false;
            }
            Individuals.Add(genome, new IndividualInGeneration(genome));
            return true;
        }

        public void RecordMatch(string a, string b, string victor, bool wentToSuddenDeath)
        {
            Debug.Log("Recording Match: " + a + " vs " + b + " victor: " + victor);

            Individuals[a].RecordMatch(b, victor, wentToSuddenDeath);
            Individuals[b].RecordMatch(a, victor, wentToSuddenDeath);
        }

        public int MinimumMatchesPlayed()
        {
            return Individuals.Values.Min(i => i.MatchesPlayed);
        }

        public IEnumerable<string> PickWinners(int WinnersCount)
        {
            return Individuals.Values.OrderBy(i => _rng.NextDouble()).OrderByDescending(i => i.GetScore()).Take(WinnersCount).Select(i => i.Genome);
        }

        /// <summary>
        /// Returns a genome from the individuals in this generation with the lowest number of completed matches.
        /// If a non-empty genome is provded, the genome provided will not be that one, or be one that has already competed with that one.
        /// </summary>
        /// <param name="genomeToCompeteWith"></param>
        /// <returns>genome of a competetor from this generation</returns>
        public string PickCompetitor(string genomeToCompeteWith = null)
        {
            List<IndividualInGeneration> validCompetitors;

            if (!string.IsNullOrEmpty(genomeToCompeteWith))
            {
                validCompetitors = Individuals.Values
                    .Where(i => i.Genome != genomeToCompeteWith && !i.PreviousCombatants.Contains(genomeToCompeteWith))
                    .OrderBy(i => i.MatchesPlayed)
                    .ThenBy(i => _rng.NextDouble())
                    .ToList();
            } else
            {
                validCompetitors = Individuals.Values
                    .OrderBy(i => i.MatchesPlayed)
                    .ThenBy(i => _rng.NextDouble())
                    .ToList();
            }

            var best = validCompetitors.FirstOrDefault();
            //Debug.Log("Picked Individual has played " + best.MatchesPlayed);
            if (best != null)
            {
                return best.Genome;
            }
            return null;
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, Individuals.Values.Select(i => i.ToString()).ToArray());
        }

        internal class IndividualInGeneration
        {
            public string Genome;

            public int Wins;
            public int SuddenDeathWins;
            public int Draws;
            public int Loses;

            public int MatchesPlayed { get { return Wins + SuddenDeathWins + Draws + Loses; } }

            public List<string> PreviousCombatants = new List<string>();
            private const int WIN_SCORE = 10;
            private const int SUDDEN_DEATH_WIN_SCORE = 7;
            private const int DRAW_SCORE = -2;
            private const int LOOSE_SCORE = -10;

            /// <summary>
            /// Construct from a generation line.
            /// If one section (i.e. no semicolons) is given, it will be interpereted as a new genome with no matches completed.
            /// </summary>
            /// <param name="line"></param>
            public IndividualInGeneration(string line)
            {
                var parts = line.Split(';');
                Genome = parts[0];
                Wins = ParsePart(parts, 1);
                SuddenDeathWins = ParsePart(parts, 2);
                Draws = ParsePart(parts, 3);
                Loses = ParsePart(parts, 4);

                if (parts.Length > 5)
                {
                    var competitorsString = parts[5];
                    PreviousCombatants = competitorsString.Split(',').ToList();
                }
            }

            public void RecordMatch(string otherCompetitor, string victor, bool wentToSuddenDeath)
            {
                PreviousCombatants.Add(otherCompetitor);

                if (Genome == victor && !wentToSuddenDeath)
                {
                    Wins++;
                }
                else if (Genome == victor && wentToSuddenDeath)
                {
                    SuddenDeathWins++;
                }
                else if (victor == otherCompetitor)
                {
                    Loses++;
                }
                else
                {
                    Draws++;
                }
            }

            private static int ParsePart(string[] parts, int index)
            {
                int retVal = 0;
                if (parts.Length > index)
                {
                    var intString = parts[index];
                    int.TryParse(intString, out retVal);
                }
                return retVal;
            }

            public override string ToString()
            {
                var competitorsString = string.Join(",", PreviousCombatants.ToArray());
                return Genome + ";" + Wins + ";" + SuddenDeathWins + ";" + Draws + ";" + Loses + ";" + competitorsString;
            }

            internal float GetScore()
            {
                var totalScore = Wins * WIN_SCORE + SuddenDeathWins * SUDDEN_DEATH_WIN_SCORE + Draws * DRAW_SCORE + Loses * LOOSE_SCORE;
                return totalScore / MatchesPlayed;
            }
        }
    }
}
