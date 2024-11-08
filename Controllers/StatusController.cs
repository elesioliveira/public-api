using Microsoft.AspNetCore.Mvc;
using rmaesolutions.configInterface;
using Npgsql;
using Serilog;
using rmaesolutions.entities;
using System.Net;
using rmaesolutions.dto;
using Minio;
using System.Text;

namespace rmaesolutions.Controllers;

[ApiController]
public class StatusController : ControllerBase
{
    /// <summary>
    /// Retorna todos os status.
    /// </summary>
    /// <returns>Uma lista de todos os status.</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///     GET /v1/status/getall
    ///     [
    ///       {
    ///         "uuid": "123e4567-e89b-12d3-a456-426614174000",
    ///         "name": "Status 1",
    ///         "description": "Descrição do Status 1",
    ///         "instruction": "Instrução do Status 1",
    ///         "createdAt": "2024-01-01T12:00:00.000000",
    ///         "updatedAt": "2024-01-01T12:00:00.000000"
    ///       },
    ///       {
    ///         "uuid": "234e5678-e89b-12d3-a456-426614174000",
    ///         "name": "Status 2",
    ///         "description": "Descrição do Status 2",
    ///         "instruction": "Instrução do Status 2",
    ///         "createdAt": "2024-01-01T12:00:00.000000",
    ///         "updatedAt": "2024-01-01T12:00:00.000000"
    ///       }
    ///     ]
    ///
    /// </remarks>
    /// <response code="200">Retorna uma lista de status.</response>
    /// <response code="204">Se a tabela Status estiver vazia.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/status/getall")]
    public IActionResult GetAll(string tenantuuid)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();



            NpgsqlCommand command = new("SELECT * FROM Status WHERE tenantuuid = @tenantuuid", connection);
            command.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));
            NpgsqlDataReader reader = command.ExecuteReader();

            if (!reader.HasRows)
            {
                return NotFound("Nenhum status cadastrado!");
            }

            List<Status> Status = [];

            while (reader.Read())
            {
                Status.Add(new Status
                {
                    Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                    Instruction = reader.IsDBNull(reader.GetOrdinal("instruction")) ? null : reader.GetString(reader.GetOrdinal("instruction")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
                });
            }

            return Ok(Status);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }

    /// <summary>
    /// Retorna um status específico.
    /// </summary>
    /// <param name="uuid">UUID do Status.</param>
    /// <returns>Retorna um objeto Status.</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///     GET /v1/status/get?uuid=123e4567-e89b-12d3-a456-426614174000
    ///     {
    ///       "uuid": "123e4567-e89b-12d3-a456-426614174000",
    ///       "name": "Status 1",
    ///       "description": "Descrição do Status 1",
    ///       "instruction": "Instrução do Status 1",
    ///       "createdAt": "2024-01-01T12:00:00.000000",
    ///       "updatedAt": "2024-01-01T12:00:00.000000"
    ///     }
    ///
    /// </remarks>
    /// <response code="200">Retorna o objeto Status.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/status/get")]
    public IActionResult Get(Guid uuid)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            NpgsqlCommand command = new("SELECT * FROM Status WHERE uuid = @uuid", connection);

            command.Parameters.AddWithValue("uuid", uuid);

            NpgsqlDataReader reader = command.ExecuteReader();

            reader.Read();

            Status Status = new()
            {
                Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                Instruction = reader.IsDBNull(reader.GetOrdinal("instruction")) ? null : reader.GetString(reader.GetOrdinal("instruction")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
            };

            return Ok(Status);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }

    /// <summary>
    /// Cria ou atualiza um status.
    /// </summary>
    /// <param name="obj">Objeto StatusDTO.</param>
    /// <param name="tenantuuid">O UUID do tenant para validar.</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro.</returns>
    /// <remarks>
    /// Exemplo de requisição para criar um status:
    ///
    ///     POST /v1/status/create
    ///     {
    ///       "name": "Novo Status",
    ///       "description": "Descrição do Novo Status",
    ///       "instruction": "Instrução do Novo Status"
    ///     }
    ///
    /// Exemplo de requisição para atualizar um status:
    ///
    ///     POST /v1/status/update
    ///     {
    ///       "uuid": "123e4567-e89b-12d3-a456-426614174000",
    ///       "name": "Status Atualizado",
    ///       "description": "Descrição do Status Atualizado",
    ///       "instruction": "Instrução do Status Atualizado"
    ///     }
    ///
    /// </remarks>
    /// <response code="200">Status cadastrado/atualizado com sucesso.</response>
    /// <response code="400">Status já cadastrado.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpPost]
    [Route("v1/status/create")]
    [Route("v1/status/update")]
    public async Task<IActionResult> Upsert([FromBody] StatusDTO obj, string tenantuuid)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            await connection.OpenAsync();

            if (obj.Uuid == null)
            {

                using (NpgsqlCommand cmd = new("SELECT * FROM Status WHERE name = @name AND tenantuuid= @tenantuuid", connection))
                {

                    cmd.Parameters.AddWithValue("name", obj.Name!.ToLower());
                    cmd.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));

                    using NpgsqlDataReader reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                    {
                        return BadRequest("Status já cadastrado");
                    }

                    reader.Close();
                }

                using (NpgsqlCommand cmd = new("INSERT INTO Status (uuid, tenantuuid, name, description, instruction, createdat, updatedat) VALUES (@uuid, @tenantuuid, @name, @description, @instruction, @createdAt, @updatedAt)", connection))
                {

                    cmd.Parameters.AddWithValue("uuid", Guid.NewGuid());
                    cmd.Parameters.AddWithValue("tenantuuid", obj.TenantUuid!);
                    cmd.Parameters.AddWithValue("name", obj.Name);
                    cmd.Parameters.AddWithValue("description", obj.Description == null ? DBNull.Value : obj.Description);
                    cmd.Parameters.AddWithValue("instruction", obj.Instruction == null ? DBNull.Value : obj.Instruction);
                    cmd.Parameters.AddWithValue("createdAt", DateTime.Now.AddHours(-3));
                    cmd.Parameters.AddWithValue("updatedAt", DateTime.Now.AddHours(-3));

                    cmd.ExecuteNonQuery();
                }

                return Ok("Status cadastrado com sucesso");

            }
            else
            {

                // REFACTOR USING STRINGBUILDER

                StringBuilder command = new("UPDATE Status SET");

                if (obj.Name != null)
                {
                    command.Append(" name = @name");
                }

                if (obj.Description != null)
                {
                    if (obj.Name != null)
                    {
                        command.Append(',');
                    }
                    command.Append("description = @description");
                }

                if (obj.Instruction != null)
                {
                    if (obj.Name != null || obj.Description != null)
                    {
                        command.Append(',');
                    }
                    command.Append("instruction = @instruction");
                }

                command.Append(", updatedat = @updatedAt");
                command.Append(" WHERE uuid = @uuid");

                using (NpgsqlCommand cmd = new(command.ToString(), connection))
                {

                    cmd.Parameters.AddWithValue("uuid", obj.Uuid);

                    if (obj.Name != null)
                    {
                        cmd.Parameters.AddWithValue("name", obj.Name);
                    }

                    if (obj.Description != null)
                    {
                        cmd.Parameters.AddWithValue("description", obj.Description);
                    }

                    if (obj.Instruction != null)
                    {
                        cmd.Parameters.AddWithValue("instruction", obj.Instruction);
                    }

                    cmd.Parameters.AddWithValue("updatedAt", DateTime.Now.AddHours(-3));

                    cmd.ExecuteNonQuery();
                }

                return Ok("Status atualizado com sucesso");
            }

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }
}
