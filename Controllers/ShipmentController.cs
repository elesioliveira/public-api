using Microsoft.AspNetCore.Mvc;
using rmaesolutions.configInterface;
using Npgsql;
using NpgsqlTypes;
using Serilog;
using rmaesolutions.entities;
using Newtonsoft.Json;
using System.Text;

namespace rmaesolutions.Controllers;

[ApiController]
public class ShipmentController : ControllerBase
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
    [Route("v1/shipments/getall")]
    public IActionResult GetAllShipments(string tenantuuid)
    {
        try
        {
            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            // Query all shipments from the table
            using NpgsqlCommand command = new NpgsqlCommand("SELECT S.*,U.USERNAME, S2.NAME FROM SHIPMENTS S INNER JOIN USERS U ON S.USERUUID = U.UUID INNER JOIN STORAGES S2 ON S.STORAGEUUID = S2.UUID WHERE S.TENANTUUID = @tenantuuid", connection);
            command.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));

            using NpgsqlDataReader reader = command.ExecuteReader();

            if (!reader.HasRows)
            {
                return NotFound("Nenhuma remessa encontrada");
            }

            List<Shipment> shipments = [];

            while (reader.Read())
            {

                Shipment shipment = new()
                {
                    Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                    TenantUuid = reader.GetGuid(reader.GetOrdinal("tenantuuid")),
                    UserUuid = reader.GetGuid(reader.GetOrdinal("useruuid")),
                    StorageUuid = reader.GetGuid(reader.GetOrdinal("storageuuid")),
                    Products = JsonConvert.DeserializeObject<List<ProductShipment>>(reader.GetString(reader.GetOrdinal("products"))),  // Atribuir a lista de produtos desserializada
                    Status = reader.GetString(reader.GetOrdinal("status")),
                    Type = reader.GetString(reader.GetOrdinal("type")),
                    StartShipmentAt = reader.IsDBNull(reader.GetOrdinal("startshipmentat")) ? null : reader.GetDateTime(reader.GetOrdinal("startshipmentat")),
                    FinishShipmentAt = reader.IsDBNull(reader.GetOrdinal("finishshipmentat")) ? null : reader.GetDateTime(reader.GetOrdinal("finishshipmentat")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat")),
                    UserName = reader.GetString(reader.GetOrdinal("username")),
                    StorageName = reader.GetString(reader.GetOrdinal("name")),
                };

                shipments.Add(shipment);
            }

            return Ok(shipments);
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
    [Route("v1/shipment/get")]
    public async Task<IActionResult> GetShipment(Guid uuid)
    {
        using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

        connection.Open();

        try
        {
            using NpgsqlCommand command = new("SELECT * FROM shipments where uuid = @uuid", connection);

            command.Parameters.AddWithValue("@uuid", uuid);

            using NpgsqlDataReader reader = command.ExecuteReader();

            if (reader.Read())
            {

                Shipment shipment = new()
                {
                    Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                    TenantUuid = reader.GetGuid(reader.GetOrdinal("tenantuuid")),
                    UserUuid = reader.GetGuid(reader.GetOrdinal("useruuid")),
                    StorageUuid = reader.GetGuid(reader.GetOrdinal("storageuuid")),
                    Products = JsonConvert.DeserializeObject<List<ProductShipment>>(reader.GetString(reader.GetOrdinal("products"))),
                    Type = reader.GetString(reader.GetOrdinal("type")),
                    Status = reader.GetString(reader.GetOrdinal("status")),
                    StartShipmentAt = reader.IsDBNull(reader.GetOrdinal("startshipmentat")) ? null : reader.GetDateTime(reader.GetOrdinal("startshipmentat")),
                    FinishShipmentAt = reader.IsDBNull(reader.GetOrdinal("finishshipmentat")) ? null : reader.GetDateTime(reader.GetOrdinal("finishshipmentat")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
                };

                return Ok(shipment);
            }

            return NotFound("Remessa não encontrada");
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
    [Route("v1/shipments/create")]
    [Route("v1/shipments/update")]
    public async Task<IActionResult> CreateShipment([FromBody] ShipmentDTO shipment)
    {
        using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);
        await connection.OpenAsync();

        try
        {
            // Se o Uuid for nulo, será uma nova remessa (CREATE)
            if (shipment.Uuid == null)
            {
                using NpgsqlCommand command = new("INSERT INTO shipments (uuid, tenantuuid, useruuid, storageuuid, products, status, type, createdat, updatedat) VALUES (@uuid, @tenantuuid, @useruuid, @storageuuid, @products, @status, @type, @createdat, @updatedat)", connection);

                Guid newUuid = Guid.NewGuid();
                command.Parameters.AddWithValue("@uuid", newUuid);
                command.Parameters.AddWithValue("@tenantuuid", shipment.TenantUuid!);
                command.Parameters.AddWithValue("@useruuid", shipment.UserUuid!);
                command.Parameters.AddWithValue("@storageuuid", shipment.StorageUuid!);
                command.Parameters.Add("@products", NpgsqlDbType.Jsonb).Value = JsonConvert.SerializeObject(shipment.Products);
                command.Parameters.AddWithValue("@status", shipment.Status!);
                command.Parameters.AddWithValue("@type", shipment.Type!);
                command.Parameters.AddWithValue("@createdat", DateTime.Now.AddHours(-3));
                command.Parameters.AddWithValue("@updatedat", DateTime.Now.AddHours(-3));

                await command.ExecuteNonQueryAsync();

                return StatusCode(201, new { message = "Remessa criada com sucesso", uuid = newUuid });
            }
            else // Se o Uuid existir, será uma atualização (UPDATE)
            {
                StringBuilder query = new("UPDATE shipments SET");

                if (shipment.TenantUuid != null) query.Append(" tenantuuid = @tenantuuid,");
                if (shipment.UserUuid != null) query.Append(" useruuid = @useruuid,");
                if (shipment.StorageUuid != null) query.Append(" storageuuid = @storageuuid,");
                if (shipment.Products != null) query.Append(" products = @products,");
                if (shipment.Status != null) query.Append(" status = @status,");
                if (shipment.Type != null) query.Append(" type = @type,");
                if (shipment.StartShipmentAt != null) query.Append(" startshipmentat = @startshipmentat,");
                if (shipment.FinishShipmentAt != null) query.Append(" finishshipmentat = @finishshipmentat,");

                query.Append(" updatedat = @updatedat");
                query.Append(" WHERE uuid = @uuid;");

                using NpgsqlCommand command = new(query.ToString(), connection);

                // Definir parâmetros apenas se não forem nulos
                if (shipment.TenantUuid != null) command.Parameters.AddWithValue("@tenantuuid", shipment.TenantUuid);
                if (shipment.UserUuid != null) command.Parameters.AddWithValue("@useruuid", shipment.UserUuid);
                if (shipment.StorageUuid != null) command.Parameters.AddWithValue("@storageuuid", shipment.StorageUuid);
                if (shipment.Products != null) command.Parameters.Add("@products", NpgsqlDbType.Jsonb).Value = JsonConvert.SerializeObject(shipment.Products);
                if (shipment.Status != null) command.Parameters.AddWithValue("@status", shipment.Status);
                if (shipment.Type != null) command.Parameters.AddWithValue("@type", shipment.Type);
                if (shipment.StartShipmentAt != null) command.Parameters.AddWithValue("@startshipmentat", shipment.StartShipmentAt);
                if (shipment.FinishShipmentAt != null) command.Parameters.AddWithValue("@finishshipmentat", shipment.FinishShipmentAt);

                command.Parameters.AddWithValue("@updatedat", DateTime.Now.AddHours(-3));
                command.Parameters.AddWithValue("@uuid", shipment.Uuid);

                await command.ExecuteNonQueryAsync();

                return Ok("Remessa atualizada com sucesso");
            }
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return StatusCode(500, "Internal Server Error");
        }
    }


}
