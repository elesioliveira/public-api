using Microsoft.AspNetCore.Mvc;
using rmaesolutions.configInterface;
using Npgsql;
using Serilog;
using rmaesolutions.entities;
using System.Net;
using rmaesolutions.dto;
using System.Text;

namespace rmaesolutions.Controllers;

[ApiController]
public class VariationController : ControllerBase
{
    /// <summary>
    /// Retorna todas as variações.
    /// </summary>
    /// <returns>Retorna uma lista de variações</returns>
    /// <remarks>
    /// Exemplo de resposta:
    /// 
    ///     [
    ///       {
    ///         "uuid": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "name": "Color",
    ///         "variationKey": "color",
    ///         "variationValue": "Red",
    ///         "createdAt": "2024-06-27T11:32:57.64126",
    ///         "updatedAt": "2024-06-27T11:33:05.003075"
    ///       },
    ///       {
    ///         "uuid": "4a95f64-5717-4562-b3fc-2c963f66afa6",
    ///         "name": "Size",
    ///         "variationKey": "size",
    ///         "variationValue": "Large",
    ///         "createdAt": "2024-06-27T12:32:57.64126",
    ///         "updatedAt": "2024-06-27T12:33:05.003075"
    ///       }
    ///     ]
    ///
    /// </remarks>
    /// <response code="200">Retorna uma lista de variações</response>
    /// <response code="204">Retorna que não há registro de variações</response>
    /// <response code="500">Retorna uma mensagem de erro</response>

    [HttpGet]
    [Route("v1/variation/getall")]
    public IActionResult GetAll(string tenantuuid)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            NpgsqlCommand command = new("SELECT * FROM variations WHERE tenantuuid = @tenantuuid", connection);
            command.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));
            NpgsqlDataReader reader = command.ExecuteReader();
            if (!reader.HasRows)
            {
                return NotFound("Nenhuma variação cadastrada!");
            }
            List<Variation> variations = [];

            while (reader.Read())
            {
                variations.Add(new Variation
                {
                    Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    VariationKey = reader.IsDBNull(reader.GetOrdinal("variationkey")) ? null : reader.GetString(reader.GetOrdinal("variationkey")),
                    VariationValue = reader.IsDBNull(reader.GetOrdinal("variationvalue")) ? null : reader.GetString(reader.GetOrdinal("variationvalue")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat")),
                });
            }

            return Ok(variations);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }

    /// <summary>
    /// Retorna uma variação.
    /// </summary>
    /// <param name="uuid">UUID da variação</param>
    /// <returns>Retorna uma variação</returns>
    /// <remarks>
    /// Exemplo de resposta:
    /// 
    ///     {
    ///         "uuid": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "name": "Color",
    ///         "variationKey": "color",
    ///         "variationValue": "Red",
    ///         "createdAt": "2024-06-27T11:32:57.64126",
    ///         "updatedAt": "2024-06-27T11:33:05.003075"
    ///     }
    /// </remarks>
    /// <response code="200">Retorna uma variação</response>
    /// <response code="500">Retorna uma mensagem de erro</response>

    [HttpGet]
    [Route("v1/variation/get")]
    public IActionResult Get(Guid uuid)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            NpgsqlCommand command = new("SELECT * FROM variations WHERE uuid = @uuid", connection);

            command.Parameters.AddWithValue("uuid", uuid);

            NpgsqlDataReader reader = command.ExecuteReader();

            reader.Read();

            Variation variation = new()
            {
                Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                VariationKey = reader.IsDBNull(reader.GetOrdinal("variationkey")) ? null : reader.GetString(reader.GetOrdinal("variationkey")),
                VariationValue = reader.IsDBNull(reader.GetOrdinal("variationvalue")) ? null : reader.GetString(reader.GetOrdinal("variationvalue")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat")),
            };

            return Ok(variation);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }

    /// <summary>
    /// Cria ou atualiza uma Variation.
    /// </summary>
    /// <param name="obj">Objeto Variation</param>
    /// <param name="tenantuuid">O UUID do tenant para validar.</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    /// <remarks>
    /// Exemplo de entrada para criação:
    /// 
    ///     POST /v1/variation/create
    ///     {
    ///         "name": "Color",
    ///         "variationKey": "color",
    ///         "variationValue": "Red"
    ///     }
    ///
    /// Exemplo de entrada para atualização:
    ///
    ///     POST /v1/variation/update
    ///     {
    ///         "uuid": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "name": "Size",
    ///         "variationKey": "size",
    ///         "variationValue": "Large"
    ///     }
    ///
    /// </remarks>
    /// <response code="200">Retorna uma mensagem de sucesso</response>
    /// <response code="500">Retorna uma mensagem de erro</response>
    [HttpPost]
    [Route("v1/variation/create")]
    [Route("v1/variation/update")]
    public async Task<IActionResult> Upsert([FromBody] VariationDTO obj, string tenantuuid)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            await connection.OpenAsync();
            if (obj.Uuid == null)
            {

                using (NpgsqlCommand cmd = new("SELECT * FROM variations WHERE name = @name AND tenantuuid= @tenantuuid", connection))
                {
                    cmd.Parameters.AddWithValue("name", obj.Name!.ToLower());
                    cmd.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));
                    using NpgsqlDataReader reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                    {
                        return BadRequest("Variação já cadastrado");
                    }

                    reader.Close();
                }

                using (NpgsqlCommand cmd = new("INSERT INTO variations (uuid,tenantuuid, name, variationkey, variationvalue, createdat, updatedat) VALUES (@uuid,@tenantuuid, @name, @variationkey, @variationvalue, @createdAt, @updatedAt)", connection))
                {

                    cmd.Parameters.AddWithValue("uuid", Guid.NewGuid());
                    cmd.Parameters.AddWithValue("tenantuuid", obj.TenantUuid!);
                    cmd.Parameters.AddWithValue("name", obj.Name);
                    cmd.Parameters.AddWithValue("variationkey", obj.VariationKey!);
                    cmd.Parameters.AddWithValue("variationvalue", obj.VariationValue!);
                    cmd.Parameters.AddWithValue("createdAt", DateTime.Now.AddHours(-3));
                    cmd.Parameters.AddWithValue("updatedAt", DateTime.Now.AddHours(-3));

                    cmd.ExecuteNonQuery();
                }

                return Ok("Variação cadastrado com sucesso");

            }
            else
            {

                // REFACTOR USING STRINGBUILDER

                StringBuilder command = new("UPDATE variations SET");

                if (obj.Name != null)
                {
                    command.Append(" name = @name");
                }

                if (obj.VariationKey != null)
                {
                    if (obj.Name != null)
                    {
                        command.Append(',');
                    }
                    command.Append("variationkey = @variationkey");
                }

                if (obj.VariationValue != null)
                {
                    if (obj.Name != null || obj.VariationKey != null)
                    {
                        command.Append(',');
                    }
                    command.Append("variationvalue = @variationvalue");
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

                    if (obj.VariationKey != null)
                    {
                        cmd.Parameters.AddWithValue("variationkey", obj.VariationKey);
                    }

                    if (obj.VariationValue != null)
                    {
                        cmd.Parameters.AddWithValue("variationvalue", obj.VariationValue);
                    }

                    cmd.Parameters.AddWithValue("updatedAt", DateTime.Now.AddHours(-3));

                    cmd.ExecuteNonQuery();
                }

                return Ok("Variação atualizado com sucesso");
            }

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }
}
