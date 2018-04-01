﻿using Assets.src.Evolution;
using Assets.Src.Database;
using Assets.Src.Evolution;
using Assets.Src.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EvolutionBrControler : BaseEvolutionController
{
    EvolutionBrConfig _config;

    protected Dictionary<string, GenomeWrapper> _currentGenomes;

    protected GenerationBr _currentGeneration;

    EvolutionBrDatabaseHandler _dbHandler;
    
    private bool _hasModules;

    protected Dictionary<string, GenomeWrapper> _extantTeams;
    public float WinBonus = 1000;

    protected List<string> _allCompetetrs { get { return _currentGenomes.Select(kv => kv.Value.Genome).ToList(); } }

    // Use this for initialization
    void Start()
    {
        DatabaseId = ArgumentStore.IdToLoad ?? DatabaseId;

        _dbHandler = new EvolutionBrDatabaseHandler();

        _config = _dbHandler.ReadConfig(DatabaseId);

        if(_config == null || _config.DatabaseId != DatabaseId)
        {
            throw new Exception("Did not retrieve expected config from database");
        }
        
        _matchControl = gameObject.AddComponent<EvolutionMatchController>();

        _mutationControl.Config = _config.MutationConfig;
        _matchControl.Config = _config.MatchConfig;
        ShipConfig.Config = _config.MatchConfig;

        ReadInGeneration();

        _hasModules = SpawnShips();
    }

    /// <summary>
    /// All the individuals still alive should be considered to have won the match.
    /// </summary>
    protected virtual bool _survivorsAreWinners { get { return _extantTeams.Count == 1; } }

    private bool _matchIsOver = false;

    [Tooltip("the number of seconds to wait after the match is over.")]
    public float WaitAtEnd = 0;

    // Update is called once per frame
    void Update()
    {
        if (_matchControl.ShouldPollForWinners() || _matchControl.IsOutOfTime() || !_hasModules)
        {
            ProcessDefeatedShips();

            //Debug.Log(_extantTeams.Count + " teams survive");   //This is wrong!
            //Debug.Log("Surviving Teams: " + string.Join(",", _extantTeams.Select(kv => kv.Key).ToArray()));
            if (_survivorsAreWinners)
            {
                //we have a winner
                foreach (var winner in _extantTeams)
                {
                    AddScoreForWinner(winner);
                }
                _matchIsOver = true;
            }
            if(_extantTeams.Count == 0)
            {
                //everyone's dead
                _matchIsOver = true;
            }
            if (_matchControl.IsOutOfTime() || !_hasModules)
            {
                //time over - draw
                //or noone has any modules, so treat it as a draw.
                AddScoreSurvivingIndividualsAtTheEnd();
                _matchIsOver = true;
            }

        }

        if (_matchIsOver)
        {
            WaitAtEnd -= Time.deltaTime;
            if(WaitAtEnd < 0)
            {
                _dbHandler.UpdateGeneration(_currentGeneration, DatabaseId, _config.GenerationNumber);

                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns>Boolean indecating that something has at least one module</returns>
    protected virtual bool SpawnShips()
    {
        var genomes = _currentGeneration.PickCompetitors(_config.NumberOfCombatants);
        
        var wrappers = new List<GenomeWrapper>();
        _currentGenomes = new Dictionary<string, GenomeWrapper>();

        var names = new List<string>();

        var i = 0;
        foreach (var g in genomes)
        {
            string name = "Nemo";
            for(var j=0; j < _matchControl.Config.CompetitorsPerTeam; j++)
            {
                var gw = ShipConfig.SpawnShip(g, i, j, _config.InSphereRandomisationRadius, _config.OnSphereRandomisationRadius);
                wrappers.Add(gw);

                name = gw.Name;

                _currentGenomes[ShipConfig.GetTag(i)] = gw; //This will only save the last gw, but they should be functionally identical.
            }

            names.Add(name);

            i++;
        }

        _extantTeams = _currentGenomes;

        Debug.Log(_config.RunName + " \"" + string.Join("\" vs \"", names.ToArray()) + "\"");

        return wrappers.Any(w => w.ModulesAdded > 0);
    }

    protected IEnumerable<string> _shipTagsPresent {
        get {
            var tags = ListShips()
            .Select(s => s.tag)
            .Distinct();
            //Debug.Log(tags.Count() + " teams still exist");
            //Debug.Log(string.Join(",", tags.ToArray()) + " teams still exist");
            return tags;
        }
    }

    protected virtual void ProcessDefeatedShips()
    {
        var tags = _shipTagsPresent;
        var nonPlayerTags = tags.Where(t => ShipConfig.Tags.Contains(t));

        if (nonPlayerTags.Count() < _extantTeams.Count)
        {
            //Something's died.
            var deadGenomes = _extantTeams.Where(kv => !nonPlayerTags.Contains(kv.Key));
            Debug.Log(deadGenomes.Count() + " genomes have died");
            foreach (var dead in deadGenomes)
            {
                AddScoreForDefeatedIndividual(dead);
            }

            _extantTeams = _currentGenomes.Where(kv => nonPlayerTags.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
        }
    }

    protected virtual void AddScoreForDefeatedIndividual(KeyValuePair<string, GenomeWrapper> deadIndividual)
    {
        Debug.Log(deadIndividual.Value.Name + " has died");
        var score = -_extantTeams.Count * _matchControl.RemainingTime();
        _currentGeneration.RecordMatch(deadIndividual.Value, score, _allCompetetrs, MatchOutcome.Loss);
    }

    protected virtual void AddScoreForWinner(KeyValuePair<string, GenomeWrapper> winner)
    {
        Debug.Log(winner.Value.Name + " Wins!");
        var score = _matchControl.RemainingTime() + WinBonus;
        _currentGeneration.RecordMatch(winner.Value, score, _allCompetetrs, MatchOutcome.Win);
    }

    protected virtual void AddScoreSurvivingIndividualsAtTheEnd()
    {
        Debug.Log("Match over: Draw. " + _extantTeams.Count + " survived.");
        var score = WinBonus / (2 * _extantTeams.Count);
        foreach (var team in _extantTeams)
        {
            _currentGeneration.RecordMatch(team.Value, score, _allCompetetrs, MatchOutcome.Draw);
        }
    }

    private void ReadInGeneration()
    {
        _currentGeneration = _dbHandler.ReadGeneration(DatabaseId, _config.GenerationNumber);

        if (_currentGeneration == null || _currentGeneration.CountIndividuals() < 2)
        {
            //The current generation does not exist - create a new random generation.
            CreateNewGeneration(null);
        }
        else if (_currentGeneration.MinimumMatchesPlayed >= _config.MinMatchesPerIndividual)
        {
            //the current generation is finished - create a new generation
            var winners = _currentGeneration.PickWinners(_config.WinnersFromEachGeneration);

            _config.GenerationNumber++;

            CreateNewGeneration(winners);
        }
        //Debug.Log("_currentGeneration: " + _currentGeneration);
    }

    /// <summary>
    /// Creates and saves a new generation in the database.
    /// If winners are provided, the new generation will be mutatnts of those.
    /// If no winners are provided, the generation number will be reset to 0, and a new default generation will be created.
    /// The current generation is set to the generation that is created.
    /// </summary>
    /// <param name="winners"></param>
    private GenerationBr CreateNewGeneration(IEnumerable<string> winners)
    {
        if (winners != null && winners.Any())
        {
            _currentGeneration = new GenerationBr(_mutationControl.CreateGenerationOfMutants(winners.ToList()));
        }
        else
        {
            Debug.Log("Generating generation from default genomes");
            _currentGeneration = new GenerationBr(_mutationControl.CreateDefaultGeneration());
            _config.GenerationNumber = 0;   //it's always generation 0 for a default genteration.
        }

        _dbHandler.SaveNewGeneration(_currentGeneration, DatabaseId, _config.GenerationNumber);
        _dbHandler.SetCurrentGenerationNumber(DatabaseId, _config.GenerationNumber);

        return _currentGeneration;
    }
}
