using Microsoft.AspNetCore.Mvc;
using rmaesolutions.configInterface;
using Npgsql;
using Serilog;
using rmaesolutions.entities;
using System.Net;

namespace rmaesolutions.Controllers;

[ApiController]
public class LabelController : ControllerBase
{
    /// <summary>
    /// Retorna todas as etiquetas.
    /// </summary>
    /// <returns>Uma lista de todas as etiquetas.</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///    [
    ///     {
    ///      "uuid": "162e3015-d1f4-43d0-96de-63178a555b3d",
    ///      "name": "NomeExemplo1",
    ///      "zpl": "ZPL1",
    ///      "height": 125,
    ///      "width": 235,
    ///      "createdAt": "2024-06-26T16:23:06.062424",
    ///      "updatedAt": "2024-06-27T11:32:44.470483"
    ///     },
    ///     {
    ///      "uuid": "4e5daec9-212d-4613-9cd7-a4cdfcdf09d3",
    ///      "name": "NomeExemplo2",
    ///      "zpl": "ZPL2",
    ///      "height": 25,
    ///      "width": 25,
    ///      "createdAt": "2024-06-27T11:32:57.64126",
    ///      "updatedAt": "2024-06-27T11:33:05.003075"
    ///     }
    ///    ]
    ///
    /// </remarks>
    /// <response code="200">Retorna todas as etiquetas cadastradas.</response>
    /// <response code="204">Se a tabela de etiquetas estiver vazia.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/labels/getall")]
    public async Task<dynamic> GetAllLabels(string tenantuuid)
    {
        try
        {
            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            await connection.OpenAsync();


            // Query all labels from the table
            using NpgsqlCommand command = new("SELECT * FROM labels WHERE tenantuuid = @tenantuuid", connection);

            command.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));

            using NpgsqlDataReader reader = command.ExecuteReader();

            if (!reader.HasRows)
            {
                return NotFound("Nenhuma etiqueta encontrada");
            }

            List<Label> labels = [];

            while (reader.Read())
            {
                Label label = new()
                {
                    Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                    TenantUuid = reader.GetGuid(reader.GetOrdinal("tenantuuid")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    ZPL = reader.GetString(reader.GetOrdinal("zpl")),
                    Height = reader.GetDouble(reader.GetOrdinal("height")),
                    Width = reader.GetDouble(reader.GetOrdinal("width")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
                };

                labels.Add(label);
            }

            return labels; // Returns the list of labels

        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return StatusCode(500, "Internal Server Error");
        }
    }

    /// <summary>
    /// Retorna uma etiqueta específica.
    /// </summary>
    /// <param name="uuid">UUID da etiqueta</param>
    /// <returns>Retorna uma etiqueta</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///    {
    ///      "uuid": "4e5daec9-212d-4613-9cd7-a4cdfcdf09d3",
    ///      "name": "NomeExemplo",
    ///      "zpl": "ZPLExemplo",
    ///      "height": 25,
    ///      "width": 25,
    ///      "createdAt": "2024-06-27T11:32:57.64126",
    ///      "updatedAt": "2024-06-27T11:33:05.003075"
    ///    }
    ///
    /// </remarks>
    /// <response code="200">Retorna a etiqueta correspondente ao UUID fornecido</response>
    /// <response code="404">Se a etiqueta não for encontrada</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>
    [HttpGet]
    [Route("v1/labels/get")]
    public async Task<dynamic> GetLabel(Guid uuid)
    {

        using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

        connection.Open();

        try
        {

            using NpgsqlCommand command = new("SELECT * FROM labels where uuid = @uuid", connection);

            command.Parameters.AddWithValue("@uuid", uuid);

            using NpgsqlDataReader reader = command.ExecuteReader();

            if (reader.Read())
            {
                Label label = new()
                {
                    Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    ZPL = reader.GetString(reader.GetOrdinal("zpl")),
                    Height = reader.GetDouble(reader.GetOrdinal("height")),
                    Width = reader.GetDouble(reader.GetOrdinal("width")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
                };

                return label;
            }

            return StatusCode(404, "Label não encontrado");
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return StatusCode(500, "Internal Server Error");
        }
    }

    /// <summary>
    /// Cria ou atualiza uma etiqueta.
    /// </summary>
    /// <param name="label">Objeto LabelDTO que contém as informações da etiqueta.</param>
    /// <returns>Retorna o status da operação.</returns>
    /// <remarks>
    /// Exemplo de entrada para criação:
    ///
    ///     {
    ///       "name": "Nome",
    ///       "zpl": "ZPL",
    ///       "height": 25,
    ///       "width": 25
    ///     }
    ///
    /// Exemplo de entrada para atualização:
    ///
    ///     {
    ///       "uuid": "4e5daec9-212d-4613-9cd7-a4cdfcdf09d3",
    ///       "name": "NomeAtualizado",
    ///       "zpl": "ZPLAtualizado",
    ///       "height": 30,
    ///       "width": 30
    ///     }
    ///
    /// </remarks>
    /// <response code="200">Etiqueta atualizada com sucesso.</response>
    /// <response code="201">Etiqueta criada com sucesso.</response>
    /// <response code="400">Se os dados fornecidos forem inválidos.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpPost]
    [Route("v1/labels/create")]
    [Route("v1/labels/update")]
    public async Task<dynamic> UpsertLabels([FromBody] LabelDTO label)
    {
        if (label == null || string.IsNullOrEmpty(label.Name) || string.IsNullOrEmpty(label.ZPL) || label.Height <= 0 || label.Width <= 0)
        {
            return BadRequest("Dados inválidos fornecidos.");
        }

        using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

        await connection.OpenAsync();
        try
        {

            if (label.Uuid == null)
            {

                using NpgsqlCommand command = new("INSERT INTO labels (uuid, tenantuuid, name, zpl, height, width, createdat, updatedat) VALUES (@uuid,@tenantuuid, @name, @zpl, @height, @width, @createdat, @updatedat)", connection);

                command.Parameters.AddWithValue("@uuid", Guid.NewGuid());
                command.Parameters.AddWithValue("@tenantuuid", label.TenantUuid!);
                command.Parameters.AddWithValue("@name", label.Name!);
                command.Parameters.AddWithValue("@zpl", label.ZPL!);
                command.Parameters.AddWithValue("@height", label.Height!);
                command.Parameters.AddWithValue("@width", label.Width!);
                command.Parameters.AddWithValue("@createdat", DateTime.Now.AddHours(-3));
                command.Parameters.AddWithValue("@updatedat", DateTime.Now.AddHours(-3));

                command.ExecuteNonQuery();

                return StatusCode(201, "Etiqueta criado com sucesso");
            }
            else
            {
                using NpgsqlCommand command = new("UPDATE labels SET name = @name, zpl = @zpl, height = @height, width = @width, updatedat = @updatedat WHERE uuid = @uuid", connection);

                command.Parameters.AddWithValue("@uuid", label.Uuid);
                command.Parameters.AddWithValue("@tenantuuid", label.TenantUuid!);
                command.Parameters.AddWithValue("@name", label.Name!);
                command.Parameters.AddWithValue("@zpl", label.ZPL!);
                command.Parameters.AddWithValue("@height", label.Height!);
                command.Parameters.AddWithValue("@width", label.Width!);
                command.Parameters.AddWithValue("@updatedat", DateTime.Now.AddHours(-3));

                command.ExecuteNonQuery();

                return StatusCode(200, "Etiqueta atualizada com sucesso");
            }
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return StatusCode(500, "Internal Server Error");
        }

    }
}
