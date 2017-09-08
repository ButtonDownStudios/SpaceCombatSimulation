﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.src.Evolution
{
    public class Generation
    {
        private Random _rng = new Random();
        private Dictionary<string, IndividualInGeneration> Individuals;

        public Generation(string[] lines)
        {
            Individuals = lines.Select(l => new IndividualInGeneration(l)).ToDictionary(i => i.Genome, i => i);
        }

        public void RecordMatch(string a, string b, string victor)
        {
            Individuals[a].RecordMatch(b, victor);
            Individuals[b].RecordMatch(a, victor);
        }

        /// <summary>
        /// Returns a genome from the individuals in this generation with the lowest number of completed matches.
        /// If a non-empty genome is provded, the genome provided will not be that one, or be one that has already competed with that one.
        /// </summary>
        /// <param name="genomeToCompeteWith"></param>
        /// <returns>genome of a competetor from this generation</returns>
        public string PickCompetitor(string genomeToCompeteWith = null)
        {
            IEnumerable<IndividualInGeneration> validCompetitors = Individuals.Values.OrderBy(i => _rng.NextDouble())
                .OrderByDescending(i => i.MatchesPlayed);

            if (!string.IsNullOrEmpty(genomeToCompeteWith))
            {
                validCompetitors = validCompetitors
                    .Where(i => i.Genome != genomeToCompeteWith && !i.PreviousCombatants.Contains(genomeToCompeteWith));
            }

            var best = validCompetitors.FirstOrDefault();
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
            public int Draws;
            public int Loses;

            public int MatchesPlayed { get { return Wins + Draws + Loses; } }

            public List<string> PreviousCombatants = new List<string>();

            public IndividualInGeneration(string line)
            {
                var parts = line.Split(';');
                Genome = parts[0];
                Wins = ParsePart(parts, 1);
                Draws = ParsePart(parts, 2);
                Loses = ParsePart(parts, 3);

                if (parts.Length > 4)
                {
                    var competitorsString = parts[4];
                    PreviousCombatants = competitorsString.Split(',').ToList();
                }
            }

            public void RecordMatch(string otherCompetitor, string victor)
            {
                PreviousCombatants.Add(otherCompetitor);

                if (Genome == victor)
                {
                    Wins++;
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
                return Genome + ";" + Wins + ";" + Draws + ";" + Loses + ";" + competitorsString;
            }
        }
    }
}
