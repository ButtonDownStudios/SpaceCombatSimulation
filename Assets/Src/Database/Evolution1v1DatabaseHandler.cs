﻿using Mono.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using UnityEngine;
using Assets.src.Evolution;
using Assets.Src.Evolution;

namespace Assets.Src.Database
{
    public class Evolution1v1DatabaseHandler
    {
        private const string DEFAULT_COMMAND_PATH = "/Database/CreateBlankDatabase.sql";
        private const string DEFAULT_DB_PATH = "/tmp/SpaceCombatSimulationDB.s3db";
        private Evolution1v1Controler _toConfigure;
        private string _connectionString
        {
            get
            {
                var connection = "URI=file:" + Application.dataPath + _databasePath;
                //Debug.Log("connection string: " + connection);
                return connection;
            }
        }
        private  string _databasePath; //Path to database.

        public Evolution1v1DatabaseHandler(Evolution1v1Controler toConfigure, string databasePath = DEFAULT_DB_PATH, string dbCreationCommandPath = DEFAULT_COMMAND_PATH)
        {
            _databasePath = databasePath;

            if (!string.IsNullOrEmpty(dbCreationCommandPath))
            {
                var initialiser = new DatabaseInitialiser
                {
                    DatabasePath = _databasePath
                };
                initialiser.EnsureDatabaseExists(dbCreationCommandPath);
            }

            _toConfigure = toConfigure;
        }

        public void ReadConfig(int id)
        {
            //Debug.Log("Reading config from DB. Id: " + id);
            using (var sql_con = new SqliteConnection(_connectionString))
            {
                IDbCommand dbcmd = null;
                IDataReader reader = null;
                try
                {
                    sql_con.Open(); //Open connection to the database.
                    dbcmd = sql_con.CreateCommand();
                    string sqlQuery = "SELECT *" +
                        " FROM EvolutionConfig1v1" +
                        " LEFT JOIN MatchConfig on MatchConfig.id = EvolutionConfig1v1.matchConfigId" +
                        " LEFT JOIN MutationConfig on MutationConfig.id = EvolutionConfig1v1.mutationConfigId" +
                        " WHERE EvolutionConfig1v1.id = " + id + ";";
                    //Debug.Log(sqlQuery);
                    dbcmd.CommandText = sqlQuery;
                    reader = dbcmd.ExecuteReader();
                    reader.Read();

                    //Debug.Log("EvolutionConfig1v1.id ordinal: " + reader.GetOrdinal("id"));
                    _toConfigure.DatabaseId = reader.GetInt32(reader.GetOrdinal("id"));

                    //Debug.Log("name ordinal: " + reader.GetOrdinal("name"));
                    _toConfigure.RunName = reader.GetString(reader.GetOrdinal("name")); //1
                    _toConfigure.GenerationNumber = reader.GetInt32(reader.GetOrdinal("currentGeneration"));
                    _toConfigure.MinMatchesPerIndividual = reader.GetInt32(reader.GetOrdinal("minMatchesPerIndividual"));
                    _toConfigure.WinnersFromEachGeneration = reader.GetInt32(reader.GetOrdinal("winnersCount"));
                    _toConfigure.SuddenDeathDamage = reader.GetFloat(reader.GetOrdinal("suddenDeathDamage"));
                    _toConfigure.SuddenDeathReloadTime = reader.GetFloat(reader.GetOrdinal("suddenDeathReloadTime"));

                    //Debug.Log("matchConfigId ordinal: " + reader.GetOrdinal("MatchConfig.Id"));  //-1
                    _toConfigure.MatchControl.Id = reader.GetInt32(7);  //TODO check this
                    _toConfigure.MatchControl.MatchTimeout = reader.GetFloat(reader.GetOrdinal("matchTimeout")); //16
                    _toConfigure.MatchControl.WinnerPollPeriod = reader.GetFloat(reader.GetOrdinal("winnerPollPeriod")); //17

                    _toConfigure.MutationControl.Id = reader.GetInt32(8);   //TODO check this
                    _toConfigure.MutationControl.Mutations = reader.GetInt32(reader.GetOrdinal("mutations"));    //19
                    _toConfigure.MutationControl.AllowedCharacters = reader.GetString(reader.GetOrdinal("allowedCharacters"));   //20
                    _toConfigure.MutationControl.MaxMutationLength = reader.GetInt32(reader.GetOrdinal("maxMutationLength"));   //21
                    _toConfigure.MutationControl.GenomeLength = reader.GetInt32(reader.GetOrdinal("genomeLength")); //22
                    _toConfigure.MutationControl.GenerationSize = reader.GetInt32(reader.GetOrdinal("generationSize"));   //23
                    _toConfigure.MutationControl.UseCompletelyRandomDefaultGenome = reader.GetBoolean(reader.GetOrdinal("randomDefault"));  //24
                    _toConfigure.MutationControl.DefaultGenome = reader.GetString(reader.GetOrdinal("defaultGenome"));   //25
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Caught exception: " + e + ", message: " + e.Message);
                    throw e;
                }
                finally
                {
                    //Debug.Log("Disconnecting");
                    if (reader != null)
                        reader.Close();
                    reader = null;
                    if (dbcmd != null)
                        dbcmd.Dispose();
                    dbcmd = null;
                    if (sql_con != null)
                        sql_con.Close();
                }
            }
        }

        public Generation1v1 ReadGeneration(int runId, int generationNumber)
        {
            //Debug.Log("Reading generation from DB. runId: " + runId + ", generation Number: " + generationNumber);
            var generation = new Generation1v1();
            using (var sql_con = new SqliteConnection(_connectionString))
            {
                IDbCommand dbcmd = null;
                IDataReader reader = null;
                try
                {
                    sql_con.Open(); //Open connection to the database.
                    dbcmd = sql_con.CreateCommand();
                    string sqlQuery = "SELECT *" +
                        " FROM Individual1v1" +
                        " WHERE runConfigId = " + runId + " AND generation = " + generationNumber +
                        ";";
                    //Debug.Log(sqlQuery);
                    dbcmd.CommandText = sqlQuery;
                    reader = dbcmd.ExecuteReader();
                    
                    while (reader.Read())
                    {
                        //Debug.Log("genome ordinal: " + reader.GetOrdinal("genome"));  //-1
                        var genome = reader.GetString(reader.GetOrdinal("genome"));

                        Debug.Log("wins ordinal: " + reader.GetOrdinal("wins"));  //-1

                        var individual = new Individual1v1(genome)
                        {
                            Score = reader.GetFloat(reader.GetOrdinal("score")),
                            Wins = reader.GetInt32(reader.GetOrdinal("wins")),
                            Loses = reader.GetInt32(reader.GetOrdinal("loses")),
                            Draws = reader.GetInt32(reader.GetOrdinal("draws")),
                            PreviousCombatantsString = reader.GetString(reader.GetOrdinal("previousCombatants")),
                        };

                        generation.Individuals.Add(individual);
                    }

                }
                catch (Exception e)
                {
                    Debug.LogWarning("Caught exception: " + e + ", message: " + e.Message);
                    throw e;
                }
                finally
                {
                    //Debug.Log("Disconnecting");
                    if (reader != null)
                        reader.Close();
                    reader = null;
                    if (dbcmd != null)
                        dbcmd.Dispose();
                    dbcmd = null;
                    if (sql_con != null)
                        sql_con.Close();
                }
            }

            return generation;
        }

        public void SaveNewGeneration(Generation1v1 generation, int runId, int generationNumber)
        {
            using (var sql_con = new SqliteConnection(_connectionString))
            {
                IDbCommand dbcmd = null;
                SqliteTransaction transaction = null;
                try
                {
                    sql_con.Open(); //Open connection to the database.

                    transaction = sql_con.BeginTransaction();
                    
                    foreach (var individual in generation.Individuals)
                    {
                        SqliteCommand insertSQL = new SqliteCommand("INSERT INTO Individual1v1 " +
                            "(runConfigId, generation, genome, score, wins, draws, loses, previousCombatants)" +
                            " VALUES (?,?,?,?,?,?,?,?)", sql_con, transaction);

                        insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)runId));
                        insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)generationNumber));
                        insertSQL.Parameters.Add(new SqliteParameter(DbType.String, (object)individual.Genome));
                        insertSQL.Parameters.Add(new SqliteParameter(DbType.Decimal, (object)individual.Score));
                        insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)individual.Wins));
                        insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)individual.Draws));
                        insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)individual.Loses));
                        insertSQL.Parameters.Add(new SqliteParameter(DbType.String, (object)individual.PreviousCombatantsString));

                        //todo check if this is nessersary/how to use transactions correctly.
                        insertSQL.Transaction = transaction;

                        insertSQL.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Caught exception: " + e + ", message: " + e.Message);
                    throw e;
                }
                finally
                {
                    //Debug.Log("Disconnecting");
                    if (transaction != null)
                        transaction.Dispose();
                    transaction = null;
                    if (dbcmd != null)
                        dbcmd.Dispose();
                    dbcmd = null;
                    if (sql_con != null)
                        sql_con.Close();
                }
            }
        }

        public void UpdateGeneration(Generation1v1 generation, int runId, int generationNumber)
        {
            using (var sql_con = new SqliteConnection(_connectionString))
            {
                IDbCommand dbcmd = null;
                SqliteTransaction transaction = null;
                try
                {
                    sql_con.Open(); //Open connection to the database.

                    transaction = sql_con.BeginTransaction();

                    foreach (var individual in generation.Individuals)
                    {
                        UpdateIndividual(individual, runId, generationNumber, sql_con, transaction);
                    }

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Caught exception: " + e + ", message: " + e.Message);
                    throw e;
                }
                finally
                {
                    //Debug.Log("Disconnecting");
                    if (transaction != null)
                        transaction.Dispose();
                    transaction = null;
                    if (dbcmd != null)
                        dbcmd.Dispose();
                    dbcmd = null;
                    if (sql_con != null)
                        sql_con.Close();
                }
            }
        }
        
        private void UpdateIndividual(Individual1v1 individual, int runId, int generationNumber, SqliteConnection sql_con, SqliteTransaction transaction)
        {
            SqliteCommand insertSQL = new SqliteCommand("UPDATE  Individual1v1 " +
                            "SET score = ?, wins = ?, draws = ?, loses = ?, previousCombatants = ?" +
                            " WHERE runConfigId = ? AND generation = ? AND genome = ?", sql_con, transaction);

            insertSQL.Parameters.Add(new SqliteParameter(DbType.Decimal, (object)individual.Score));
            insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)individual.Wins));
            insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)individual.Draws));
            insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)individual.Loses));
            insertSQL.Parameters.Add(new SqliteParameter(DbType.String, (object)individual.PreviousCombatantsString));

            insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)runId));
            insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)generationNumber));
            insertSQL.Parameters.Add(new SqliteParameter(DbType.String, (object)individual.Genome));

            //todo check if this is nessersary/how to use transactions correctly.
            insertSQL.Transaction = transaction;

            insertSQL.ExecuteNonQuery();
        }

        public Generation1v1 ReadCurrentGeneration()
        {
            return ReadGeneration(_toConfigure.DatabaseId, _toConfigure.GenerationNumber);
        }

        public void SetCurrentGeneration(int generationNumber)
        {
            _toConfigure.GenerationNumber = generationNumber;

            using (var sql_con = new SqliteConnection(_connectionString))
            {
                sql_con.Open();
                SqliteCommand command = null;

                try
                {
                    //Debug.Log("Updating generation to " + _toConfigure.GenerationNumber);
                    command = new SqliteCommand("UPDATE EvolutionConfig1v1 SET currentGeneration = ? WHERE id = ?;", sql_con);

                    command.Parameters.Add(new SqliteParameter(DbType.Int32, (object)_toConfigure.GenerationNumber));
                    command.Parameters.Add(new SqliteParameter(DbType.Int32, (object)_toConfigure.DatabaseId));

                    command.ExecuteNonQuery();
                }
                finally
                {
                    if (command != null)
                        command.Dispose();
                    if (sql_con != null)
                        sql_con.Close();

                }
            }
        }

        public void SaveConfig()
        {
            throw new NotImplementedException();
            //using (var sql_con = new SqliteConnection(_connectionString))
            //{
            //    sql_con.Open();
            //    try
            //    {
            //        SaveMatchConfig(_toConfigure.MatchControl, sql_con);
            //        SaveMutationConfig(_toConfigure.MutationControl, sql_con);

            //        SaveEvolutionControlerConfig(sql_con);
            //    }
            //    finally
            //    {
            //        if (sql_con != null)
            //            sql_con.Close();
            //    }
            //}
        }

        private int? SaveMatchConfig(EvolutionMatchController matchConfig, SqliteConnection sql_con)
        {
            throw new NotImplementedException();
            //if (matchConfig.Id.HasValue)
            //{
            //    Debug.LogWarning("Updating existing MatchConfig not implemented, Id: " + matchConfig.Id.Value);
            //}
            //else
            //{
            //    string sqlQuery = "SELECT id" +
            //    " FROM MatchConfig;";

            //    var dbcmd = sql_con.CreateCommand();
            //    dbcmd.CommandText = sqlQuery;
            //    var reader = dbcmd.ExecuteReader();

            //    var ids = new List<int>();
            //    while (reader.Read())
            //    {
            //        ids.Add(reader.GetInt32(0));
            //    }
            //    var maxId = ids.Max();
            //    var newId = maxId + 1;

            //    matchConfig.Id = newId;

            //    SqliteCommand insertSQL = new SqliteCommand("INSERT INTO MatchConfig (id, matchTimeout, winnerPollPeriod) VALUES (?,?,?)", sql_con);

            //    insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)matchConfig.Id));
            //    insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)matchConfig.MatchTimeout));
            //    insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)matchConfig.WinnerPollPeriod));

            //    insertSQL.ExecuteNonQuery();
            //}
            //return matchConfig.Id;
        }

        private int? SaveMutationConfig(EvolutionMutationController mutationConfig, SqliteConnection sql_con)
        {
            throw new NotImplementedException();
            //if (mutationConfig.Id.HasValue)
            //{
            //    Debug.LogWarning("Updating existing MutationConfig not implemented, Id: " + mutationConfig.Id.Value);
            //}
            //else
            //{
            //    sql_con = new SqliteConnection(_connectionString);
            //    sql_con.Open(); //Open connection to the database.

            //    string sqlQuery = "SELECT id" +
            //    " FROM MutationConfig;";

            //    var dbcmd = sql_con.CreateCommand();
            //    dbcmd.CommandText = sqlQuery;
            //    var reader = dbcmd.ExecuteReader();

            //    var ids = new List<int>();
            //    while (reader.Read())
            //    {
            //        ids.Add(reader.GetInt32(0));
            //    }
            //    var maxId = ids.Max();
            //    var newId = maxId + 1;
            //    mutationConfig.Id = newId;

            //    SqliteCommand insertSQL = new SqliteCommand("INSERT INTO MutationConfig (" +
            //        "id, mutations, allowedCharacters, maxMutationLength" +
            //        ", genomeLength, generationSize, randomDefault, defaultGenome) VALUES (?,?,?,?,?,?,?,?)", sql_con);

            //    insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)mutationConfig.Id.Value));
            //    insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)mutationConfig.Mutations));
            //    insertSQL.Parameters.Add(new SqliteParameter(DbType.String, (object)mutationConfig.AllowedCharacters));
            //    insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)mutationConfig.MaxMutationLength));
            //    insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)mutationConfig.GenomeLength));
            //    insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)mutationConfig.GenerationSize));
            //    insertSQL.Parameters.Add(new SqliteParameter(DbType.Boolean, (object)mutationConfig.UseCompletelyRandomDefaultGenome));
            //    insertSQL.Parameters.Add(new SqliteParameter(DbType.String, (object)mutationConfig.DefaultGenome));

            //    insertSQL.ExecuteNonQuery();

            //}
            //return mutationConfig.Id;
        }

        private void SaveEvolutionControlerConfig(SqliteConnection sql_con)
        {
            throw new NotImplementedException();
            //if (_toConfigure.ReadFromDatabase)
            //{
            //    //If it's set to be read from the database, it must already exist in the database, so update the existing one.
            //    Debug.LogWarning("Updating existing EvolutionConfig1v1 not implemented, Id: " + _toConfigure.DatabaseId);
            //}
            //else
            //{
            //    SqliteCommand insertSQL = new SqliteCommand("INSERT INTO EvolutionConfig1v1 (name , currentGeneration , minMatchesPerIndividual" +
            //        ", winnersCount , minDrones , droneEscalation , maxDrones , killScoreMultiplier, flatKillBonus, completionBonus " +
            //        ", deathPenalty, droneList, matchConfigId, mutationConfigId) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?)", sql_con);

            //    //insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)_toConfigure.Id.Value));
            //    insertSQL.Parameters.Add(new SqliteParameter(DbType.String, (object)_toConfigure.RunName));
            //    insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)_toConfigure.GenerationNumber));
            //    insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)_toConfigure.MinMatchesPerIndividual));
            //    insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)_toConfigure.WinnersFromEachGeneration));
            //    insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)_toConfigure.MinDronesToSpawn));
            //    insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)_toConfigure.ExtraDroneEveryXGenerations));
            //    insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)_toConfigure.MaxDronesToSpawn));
            //    insertSQL.Parameters.Add(new SqliteParameter(DbType.Decimal, (object)_toConfigure.KillScoreMultiplier));
            //    insertSQL.Parameters.Add(new SqliteParameter(DbType.Decimal, (object)_toConfigure.FlatKillBonus));
            //    insertSQL.Parameters.Add(new SqliteParameter(DbType.Decimal, (object)_toConfigure.CompletionBonus));
            //    insertSQL.Parameters.Add(new SqliteParameter(DbType.Decimal, (object)_toConfigure.DeathPenalty));
            //    insertSQL.Parameters.Add(new SqliteParameter(DbType.String, (object)_toConfigure.DronesString));

            //    insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)_toConfigure.MatchControl.Id));
            //    insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)_toConfigure.MutationControl.Id));

            //    insertSQL.ExecuteNonQuery();
            //}
        }
    }
}