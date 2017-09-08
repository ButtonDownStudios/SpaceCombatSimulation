﻿using Assets.src.Evolution;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using Assets.Src.ObjectManagement;

public class EvolutionControler : MonoBehaviour
{
    public Rigidbody ShipToEvolve;
    public Transform Location1;
    public Transform Location2;
    public bool RandomiseRotation = true;
    public float LocationRandomisationRadius = 0;
    public string Tag1 = "Team1";
    public string Tag2 = "Team2";
    public string CurrentGenerationFilePath = "./tmp/evolvingShips/currentGeneration.txt";
    public string GenerationFilePathBase = "./tmp/evolvingShips/Generations/G-";

    public string SpaceShipTag = "SpaceShip";
    private Dictionary<string, string> _currentGenomes;

    public int GenerationSize = 10;

    /// <summary>
    /// The generation is over when every individual has had at least this many matches.
    /// </summary>
    public int MinMatchesPerIndividual = 3;

    /// <summary>
    /// The number of individuals to keep for the next generation
    /// </summary>
    public int WinnersFromEachGeneration = 3;

    public int MatchTimeout = 10000;

    public int Mutations = 3;
    
    public int MaxTurrets = 10;
    public string AllowedCharacters = " 0123456789  ";
    
    public int MaxMutationLength = 5;
    
    public int MaxShootAngle = 180;
    public int MaxTorqueMultiplier = 2000;
    public int MaxLocationAimWeighting = 10;
    public int MaxSlowdownWeighting = 60;
    public int MaxLocationTollerance = 1000;
    public int MaxVelociyTollerance = 200;
    public int MaxAngularDragForTorquers = 1;

    public int GenomeLength = 50;
    
    public List<Rigidbody> Modules;
    private string DrawKeyword = "DRAW";
    private StringMutator _mutator;
    public string DefaultGenome = "";
    private int GenerationNumber;
    private Generation _currentGeneration;

    public Rigidbody SuddenDeathObject;
    public int SuddenDeathObjectReloadTime = 200;
    public float SuddenDeathSpawnSphereRadius = 1000;

    // Use this for initialization
    void Start()
    {
        _mutator = new StringMutator();
        ReadCurrentGeneration();
        SpawnShips();
    }

    // Update is called once per frame
    void Update()
    {
        var winningGenome = DetectVictorsGenome();
        if (winningGenome == null && MatchTimeout > 0)
        {
            MatchTimeout--;
            return;
        }
        else if (MatchTimeout <= 0)
        {
            Debug.Log("Match Timeout!");
            if(SuddenDeathObject != null)
            {
                ActivateSuddenDeath();
            } else
            {
                winningGenome = string.Empty;
            }
        }

        if (winningGenome != null)
        {
            Debug.Log("\"" + winningGenome + "\" Wins!");
            var a = _currentGenomes.Keys.First();
            var b = _currentGenomes.Keys.Skip(1).First();

            _currentGeneration.RecordMatch(a, b, winningGenome);
        }
        
        SaveGeneration();

        PrepareForNextMatch();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ActivateSuddenDeath()
    {
        Debug.Log("Sudden Death!");
        var orientation = UnityEngine.Random.rotation;
        var randomPlacement = (SuddenDeathSpawnSphereRadius * UnityEngine.Random.insideUnitSphere) + transform.position;
        var death = Instantiate(SuddenDeathObject, randomPlacement, orientation);
        death.SendMessage("SetEnemyTags", new List<string> { Tag1, Tag2 });
        MatchTimeout = SuddenDeathObjectReloadTime;
    }

    private void PrepareForNextMatch()
    {
        if(_currentGeneration.MinimumMatchesPlayed() >= MinMatchesPerIndividual)
        {
            //should move to next generation
            var winners = _currentGeneration.PickWinners(WinnersFromEachGeneration);
            GenerationNumber = GenerationNumber+1;
            _currentGeneration = CreateGenerationOfMutants(winners.ToList());
            SaveGeneration();
        }
    }

    private void SaveGeneration()
    {
        string path = PathForThisGeneration();
        Debug.Log("Saving to " + Path.GetFullPath(path));
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
        }
        File.WriteAllText(path, _currentGeneration.ToString());
        File.WriteAllText(CurrentGenerationFilePath, GenerationNumber.ToString());
    }

    private string PathForThisGeneration()
    {
        var generationFilePath = GenerationFilePathBase + (GenerationNumber.ToString().PadLeft(6, '0'));
        return generationFilePath;
    }

    private void SpawnShips()
    {
        var genomes = PickTwoGenomesFromHistory();

        Debug.Log("\"" + string.Join("\" vs \"", genomes.ToArray()) + "\"");

        var g1 = genomes[0];
        var g2 = genomes[1];

        SpawnShip(g1, Tag1, Tag2, Location1);
        SpawnShip(g2, Tag2, Tag1, Location2);

        _currentGenomes = new Dictionary<string, string>
        {
            {Tag1,g1 },
            {Tag2,g2 }
        };
    }

    private void SpawnShip(string genome, string ownTag, string enemyTag, Transform location)
    {
        var orientation = RandomiseRotation ? UnityEngine.Random.rotation : location.rotation;
        var randomPlacement = (LocationRandomisationRadius * UnityEngine.Random.insideUnitSphere) + location.position;
        var ship = Instantiate(ShipToEvolve, randomPlacement, orientation);
        ship.tag = ownTag;
        var enemyTags = new List<string> { enemyTag };

        new ShipBuilder(genome, ship.transform, Modules)
        {
            MaxShootAngle = MaxShootAngle,
            MaxTorqueMultiplier = MaxTorqueMultiplier,
            MaxLocationAimWeighting = MaxLocationAimWeighting,
            MaxSlowdownWeighting = MaxSlowdownWeighting,
            MaxLocationTollerance = MaxLocationTollerance,
            MaxVelociyTollerance = MaxVelociyTollerance,
            MaxAngularDragForTorquers = MaxAngularDragForTorquers,
            EnemyTags = enemyTags,
            MaxTurrets = MaxTurrets
        }.BuildShip();

        ship.SendMessage("SetEnemyTags", enemyTags);
    }
    
    /// <summary>
    /// Returns the genome of the victor.
    /// Or null if there's no victor yet.
    /// Or empty string if everyone's dead.
    /// </summary>
    /// <returns></returns>
    private string DetectVictorsGenome()
    {
        var tags = GameObject.FindGameObjectsWithTag(SpaceShipTag)
            .Where(s =>
                s.transform.parent != null &&
                s.transform.parent.GetComponent("Rigidbody") != null
            )
            .Select(s => s.transform.parent.tag)
            .Distinct();
        //Debug.Log(ships.Count() + " ships exist");

        if (tags.Count() == 1)
        {
            var winningTag = tags.First();

            //Debug.Log(StringifyGenomes() + " winning tag: " + winningTag);
            return _currentGenomes[winningTag];
        }
        if (tags.Count() == 0)
        {
            Debug.Log("Everyone's dead!");
            return string.Empty;
        }
        return null;
    }
        
    private string[] PickTwoGenomesFromHistory()
    {
        var g1 = _currentGeneration.PickCompetitor();
        var g2 = _currentGeneration.PickCompetitor(g1);
        return new string[] { g1, g2 };
    }

    private void ReadCurrentGeneration()
    {

        if (File.Exists(CurrentGenerationFilePath))
        {
            var GenerationNumberText = File.ReadAllText(CurrentGenerationFilePath);
            if(!int.TryParse(GenerationNumberText, out GenerationNumber))
            {
                GenerationNumber = 0;
            }
            string path = PathForThisGeneration();
            
            var lines = File.ReadAllLines(path);
            _currentGeneration = new Generation(lines);
        } else
        {
            Debug.Log("Current generation File not found mutating default for new generation");
        }
        if(_currentGeneration.CountIndividuals() < 2)
        {
            _currentGeneration = CreateGenerationOfMutants(new List<string> { DefaultGenome });
        }
    }

    private Generation CreateGenerationOfMutants(List<string> baseGenomes)
    {
        var genration = new Generation();
        int i = 0;
        while(genration.CountIndividuals() < GenerationSize)
        {
            var baseGenome = baseGenomes[i];
            var mutant = _mutator.Mutate(baseGenome);
            genration.AddGenome(mutant);
            i++;
            i = i % baseGenomes.Count;
        }
        return genration;
    }
}
