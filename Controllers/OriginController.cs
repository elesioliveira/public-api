using Microsoft.AspNetCore.Mvc;
using rmaesolutions.configInterface;
using Npgsql;
using Serilog;
using rmaesolutions.entities;
using System.Net;
using rmaesolutions.dto;

namespace rmaesolutions.Controllers;

[ApiController]
public class OriginController : ControllerBase
{
    /// <summary>
    /// Retorna todas as origens.
    /// </summary>
    /// <returns>Uma lista de todas as origens.</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///    [
    ///     {
    ///      "uuid": "a3f1c96d-75b4-4b6a-baf3-61b91c478a9a",
    ///      "name": "Origem 1",
    ///      "description": "Descrição da Origem 1",
    ///      "createdAt": "2024-01-01T12:00:00.000000",
    ///      "updatedAt": "2024-01-01T12:00:00.000000"
    ///     },
    ///     {
    ///      "uuid": "b2d7e56d-8f4d-4d6a-bfc3-62b71c478a9b",
    ///      "name": "Origem 2",
    ///      "description": "Descrição da Origem 2",
    ///      "createdAt": "2024-01-02T12:00:00.000000",
    ///      "updatedAt": "2024-01-02T12:00:00.000000"
    ///     }
    ///    ]
    ///
    /// </remarks>
    /// <response code="200">Retorna todas as origens cadastradas.</response>
    /// <response code="204">Se a tabela de origens estiver vazia.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/origin/getall")]
    public IActionResult GetAll(string tenantuuid)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();


            NpgsqlCommand command = new("SELECT * FROM origins WHERE tenantuuid = @tenantuuid", connection);

            command.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));

            NpgsqlDataReader reader = command.ExecuteReader();

            if (!reader.HasRows)
            {
                return NotFound("Nenhuma origem cadastrada");
            }

            List<Origin> origins = [];

            while (reader.Read())
            {
                origins.Add(new Origin
                {
                    Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Description = reader.GetString(reader.GetOrdinal("description")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
                });
            }

            return Ok(origins);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }

    /// <summary>
    /// Retorna uma origem específica.
    /// </summary>
    /// <param name="uuid">UUID da origem</param>
    /// <returns>Retorna uma origem</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///    {
    ///      "uuid": "a3f1c96d-75b4-4b6a-baf3-61b91c478a9a",
    ///      "name": "Origem 1",
    ///      "description": "Descrição da Origem 1",
    ///      "createdAt": "2024-01-01T12:00:00.000000",
    ///      "updatedAt": "2024-01-01T12:00:00.000000"
    ///    }
    ///
    /// </remarks>
    /// <response code="200">Retorna a origem correspondente ao UUID fornecido</response>
    /// <response code="404">Se a origem não for encontrada</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/origin/get")]
    public IActionResult Get(Guid uuid)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            NpgsqlCommand command = new("SELECT * FROM origins WHERE uuid = @uuid", connection);

            command.Parameters.AddWithValue("uuid", uuid);

            NpgsqlDataReader reader = command.ExecuteReader();

            reader.Read();

            Origin origin = new()
            {
                Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
            };

            return Ok(origin);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }

    /// <summary>
    /// Cria ou atualiza uma Origem.
    /// </summary>
    /// <param name="obj">Objeto OrigemDTO contendo as informações da origem.</param>
    /// <param name="tenantuuid">O UUID do tenant para validar.</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro.</returns>
    /// <remarks>
    /// Exemplo de entrada para criação:
    ///
    ///     POST /v1/origin/create
    ///     {
    ///       "name": "Nova Origem",
    ///       "description": "Descrição da nova origem"
    ///     }
    ///
    /// Exemplo de entrada para atualização:
    ///
    ///     POST /v1/origin/update
    ///     {
    ///       "uuid": "a3f1c96d-75b4-4b6a-baf3-61b91c478a9a",
    ///       "name": "Origem Atualizada",
    ///       "description": "Descrição atualizada da origem"
    ///     }
    ///
    /// </remarks>
    /// <response code="200">Retorna uma mensagem de sucesso.</response>
    /// <response code="400">Se a origem já estiver cadastrada.</response>
    /// <response code="500">Retorna uma mensagem de erro.</response>

    [HttpPost]
    [Route("v1/origin/create")]
    [Route("v1/origin/update")]
    public IActionResult Upsert([FromBody] OriginDTO obj, string tenantuuid)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            if (obj.Uuid == null)
            {

                using (NpgsqlCommand cmd = new("SELECT * FROM origins WHERE name = @name AND tenantuuid= @tenantuuid", connection))
                {

                    cmd.Parameters.AddWithValue("name", obj.Name!.ToLower());
                    cmd.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));
                    using NpgsqlDataReader reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                    {
                        return BadRequest("Origem já cadastrada");
                    }

                    reader.Close();
                }

                using (NpgsqlCommand cmd = new("INSERT INTO origins (uuid, tenantuuid, name, description, createdat, updatedat) VALUES (@uuid, @tenantuuid, @name, @description, @createdat, @updatedat)", connection))
                {

                    cmd.Parameters.AddWithValue("uuid", Guid.NewGuid());
                    cmd.Parameters.AddWithValue("tenantuuid", obj.TenantUuid!);
                    cmd.Parameters.AddWithValue("name", obj.Name!.ToLower());
                    cmd.Parameters.AddWithValue("description", obj.Description == null ? DBNull.Value : obj.Description);
                    cmd.Parameters.AddWithValue("createdat", DateTime.Now.AddHours(-3));
                    cmd.Parameters.AddWithValue("updatedat", DateTime.Now.AddHours(-3));

                    cmd.ExecuteNonQuery();
                }

                return Ok("Origem cadastrada com sucesso");

            }
            else
            {

                string command = "UPDATE origins SET";

                if (obj.Name != null)
                {
                    command += " name = @name";
                }

                if (obj.Description != null)
                {
                    if (obj.Name != null)
                    {
                        command += ",";
                    }

                    command += "description = @description";
                }

                command += ", updatedat = @updatedat";
                command += " WHERE uuid = @uuid";

                using (NpgsqlCommand cmd = new(command, connection))
                {

                    cmd.Parameters.AddWithValue("uuid", obj.Uuid);
                    if (obj.Name != null)
                    {
                        cmd.Parameters.AddWithValue("name", obj.Name!);
                    }
                    if (obj.Description != null)
                    {
                        cmd.Parameters.AddWithValue("description", obj.Description!);
                    }

                    cmd.Parameters.AddWithValue("updatedat", DateTime.Now.AddHours(-3));

                    cmd.ExecuteNonQuery();
                }

                return Ok("Origem atualizada com sucesso");
            }



        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }
}
