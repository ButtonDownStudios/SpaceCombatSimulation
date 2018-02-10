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
    public class Evolution1v1DatabaseHandler : GeneralDatabaseHandler
    {
        private const string CONFIG_TABLE = "EvolutionConfig1v1";
        private const string INDIVIDUAL_TABLE = "Individual1v1";
        protected override string RUN_TYPE_NAME { get { return "1v1"; } }

        public Evolution1v1DatabaseHandler(string databasePath, string dbCreationCommandPath):base(databasePath, dbCreationCommandPath)
        {
        }

        public Evolution1v1DatabaseHandler(string databasePath) : base(databasePath)
        {
        }

        public Evolution1v1DatabaseHandler() : base()
        {
        }

        public override Dictionary<int, string> ListConfigs()
        {
            return ListConfigs(CONFIG_TABLE);
        }

        public Evolution1v1Config ReadConfig(int id)
        {
            var config = new Evolution1v1Config();

            //Debug.Log("Reading config from DB. Id: " + id);
            using (var sql_con = new SqliteConnection(_connectionString))
            {
                IDbCommand dbcmd = null;
                IDataReader reader = null;
                try
                {
                    reader = OpenReaderWithCommand(sql_con, CreateReadConfigQuery(CONFIG_TABLE, id), out dbcmd);

                    if (reader.Read())
                    {
                        //Debug.Log("EvolutionConfig1v1.id ordinal: " + reader.GetOrdinal("id"));
                        config.DatabaseId = reader.GetInt32(reader.GetOrdinal("id"));

                        //Debug.Log("suddenDeathReloadTime ordinal: " + reader.GetOrdinal("suddenDeathReloadTime"));
                        //Debug.Log("suddenDeathReloadTime value: " + reader.GetDecimal(reader.GetOrdinal("suddenDeathReloadTime")));

                        //Debug.Log("matchConfigId ordinal: " + reader.GetOrdinal("matchConfigId"));
                        //Debug.Log("matchConfigId value: " + reader.GetDecimal(reader.GetOrdinal("matchConfigId")));

                        config.RunName = reader.GetString(reader.GetOrdinal("name")); //1
                        config.GenerationNumber = reader.GetInt32(reader.GetOrdinal("currentGeneration"));
                        config.MinMatchesPerIndividual = reader.GetInt32(reader.GetOrdinal("minMatchesPerIndividual"));
                        config.WinnersFromEachGeneration = reader.GetInt32(reader.GetOrdinal("winnersCount"));
                        config.SuddenDeathDamage = reader.GetFloat(reader.GetOrdinal("suddenDeathDamage"));
                        config.SuddenDeathReloadTime = reader.GetFloat(reader.GetOrdinal("suddenDeathReloadTime"));

                        config.MatchConfig = ReadMatchConfig(reader, reader.GetOrdinal("matchConfigId"));
                        config.MutationConfig = ReadMutationConfig(reader, reader.GetOrdinal("mutationConfigId"));
                    } else
                    {
                        throw new Exception("Config not founr for ID " + id);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Caught exception: " + e + ", message: " + e.Message);
                    throw e;
                }
                finally
                {
                    Disconnect(reader, null, dbcmd, sql_con);
                }
                return config;
            }
        }

        public int UpdateExistingConfig(Evolution1v1Config config)
        {
            using (var sql_con = new SqliteConnection(_connectionString))
            {
                IDbCommand dbcmd = null;
                SqliteTransaction transaction = null;
                try
                {
                    sql_con.Open(); //Open connection to the database.

                    transaction = sql_con.BeginTransaction();
                    
                    UpdateExistingMatchConfig(config.MatchConfig, sql_con, transaction);
                    UpdateExistingMutationConfig(config.MutationConfig, sql_con, transaction);

                    SqliteCommand insertSQL = new SqliteCommand(sql_con)
                    {
                        Transaction = transaction
                    };
                    
                    insertSQL.CommandText = "UPDATE " + CONFIG_TABLE +
                        " SET name = ?, currentGeneration = ?, minMatchesPerIndividual = ?, winnersCount = ?, suddenDeathDamage = ?, suddenDeathReloadTime = ?" +
                        " WHERE id = ?";

                    insertSQL.Parameters.Add(new SqliteParameter(DbType.String, (object)config.RunName));
                    insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)config.GenerationNumber));
                    insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)config.MinMatchesPerIndividual));
                    insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)config.WinnersFromEachGeneration));
                    insertSQL.Parameters.Add(new SqliteParameter(DbType.Decimal, (object)config.SuddenDeathDamage));
                    insertSQL.Parameters.Add(new SqliteParameter(DbType.Decimal, (object)config.SuddenDeathReloadTime));

                    insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)config.DatabaseId));

                    insertSQL.ExecuteNonQuery();

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Caught exception: " + e + ", message: " + e.Message);
                    throw e;
                }
                finally
                {
                    Disconnect(null, transaction, dbcmd, sql_con);
                }
            }

            return config.DatabaseId;
        }

        public int SaveNewConfig(Evolution1v1Config config)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                SqliteTransaction transaction = null;
                try
                {
                    connection.Open(); //Open connection to the database.

                    transaction = connection.BeginTransaction();
                    
                    config.MatchConfig.Id = SaveMatchConfig(config.MatchConfig, connection, transaction);
                    config.MutationConfig.Id = SaveMutationConfig(config.MutationConfig, connection, transaction);

                    SqliteCommand insertSQL = new SqliteCommand(connection)
                    {
                        Transaction = transaction
                    };

                    insertSQL.CommandText = "INSERT INTO " + CONFIG_TABLE +
                        "(name, currentGeneration, minMatchesPerIndividual, winnersCount, suddenDeathDamage, suddenDeathReloadTime, matchConfigId, mutationConfigId)" +
                        " VALUES (?,?,?,?,?,?,?,?)";

                    insertSQL.Parameters.Add(new SqliteParameter(DbType.String, (object)config.RunName));
                    insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)config.GenerationNumber));
                    insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)config.MinMatchesPerIndividual));
                    insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)config.WinnersFromEachGeneration));
                    insertSQL.Parameters.Add(new SqliteParameter(DbType.Decimal, (object)config.SuddenDeathDamage));
                    insertSQL.Parameters.Add(new SqliteParameter(DbType.Decimal, (object)config.SuddenDeathReloadTime));

                    insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)config.MatchConfig.Id));
                    insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)config.MutationConfig.Id));

                    insertSQL.ExecuteNonQuery();
                    insertSQL.Dispose();

                    SqliteCommand readIdCommand = new SqliteCommand(connection)
                    {
                        Transaction = transaction
                    };

                    //From http://www.sliqtools.co.uk/blog/technical/sqlite-how-to-get-the-id-when-inserting-a-row-into-a-table/
                    readIdCommand.CommandText = "select last_insert_rowid()";

                    // The row ID is a 64-bit value - cast the Command result to an Int64.
                    //
                    var LastRowID64 = (Int64)readIdCommand.ExecuteScalar();
                    readIdCommand.Dispose();

                    // Then grab the bottom 32-bits as the unique ID of the row.
                    //
                    int LastRowID = (int)LastRowID64;
                    //end of copied code.
                    
                    config.DatabaseId = LastRowID;

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Caught exception: " + e + ", message: " + e.Message);
                    throw e;
                }
                finally
                {
                    Disconnect(null, transaction, null, connection);
                }
            }

            return config.DatabaseId;
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
                    reader = OpenReaderWithCommand(sql_con, CreateReadIndividualsQuery(INDIVIDUAL_TABLE, runId, generationNumber), out dbcmd);
                    
                    while (reader.Read())
                    {
                        //Debug.Log("wins ordinal: " + reader.GetOrdinal("wins"));

                        var individual = new Individual1v1(ReadSpeciesSummary(reader))
                        {
                            Score = reader.GetFloat(reader.GetOrdinal("score")),
                            Wins = reader.GetInt32(reader.GetOrdinal("wins")),
                            Loses = reader.GetInt32(reader.GetOrdinal("loses")),
                            Draws = reader.GetInt32(reader.GetOrdinal("draws")),
                            PreviousCombatantsString = reader.GetString(reader.GetOrdinal("previousCombatants"))
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
                    Disconnect(reader, null, dbcmd, sql_con);
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
                        SaveBaseIndividual(RUN_TYPE_NAME, individual, runId, generationNumber, sql_con, transaction);

                        SqliteCommand insertSQL = new SqliteCommand("INSERT INTO Individual1v1 " +
                            "(runConfigId, generation, genome, wins, draws, loses, previousCombatants)" +
                            " VALUES (?,?,?,?,?,?,?)", sql_con, transaction);

                        insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)runId));
                        insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)generationNumber));
                        insertSQL.Parameters.Add(new SqliteParameter(DbType.String, (object)individual.Genome));
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
                    Debug.LogWarning(e.StackTrace);
                    throw e;
                }
                finally
                {
                    Disconnect(null, transaction, dbcmd, sql_con);
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
                    Disconnect(null, transaction, dbcmd, sql_con);
                }
            }
        }
        
        private void UpdateIndividual(Individual1v1 individual, int runId, int generationNumber, SqliteConnection sql_con, SqliteTransaction transaction)
        {
            UpdateBaseIndividual(individual, runId, generationNumber, sql_con, transaction);

            SqliteCommand insertSQL = new SqliteCommand("UPDATE  Individual1v1" +
                            " SET wins = ?, draws = ?, loses = ?, previousCombatants = ?" +
                            " WHERE runConfigId = ? AND generation = ? AND genome = ?", sql_con, transaction);
            
            insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)individual.Wins));
            insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)individual.Draws));
            insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)individual.Loses));
            insertSQL.Parameters.Add(new SqliteParameter(DbType.String, (object)individual.PreviousCombatantsString));

            insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)runId));
            insertSQL.Parameters.Add(new SqliteParameter(DbType.Int32, (object)generationNumber));
            insertSQL.Parameters.Add(new SqliteParameter(DbType.String, (object)individual.Genome));
            
            insertSQL.ExecuteNonQuery();
        }

        public void SetCurrentGenerationNumber(int databaseId, int generationNumber)
        {
            SetCurrentGenerationNumber(CONFIG_TABLE, databaseId, generationNumber);
        }
    }
}
