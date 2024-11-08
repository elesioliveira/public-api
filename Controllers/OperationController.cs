using System.Net;
using Serilog;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using rmaesolutions.configInterface;
using rmaesolutions.dto;
using System.Text.Json;
using System.Text;
using rmaesolutions.entities;

namespace rmaesolutions.Controllers;

[ApiController]
public class OperationController : ControllerBase
{
    /// <summary>
    /// Retorna o status de um produto com base no número de série ou código de rastreamento.
    /// </summary>
    /// <param name="serial">Número de série do produto (opcional).</param>
    /// <param name="trackingcode">Código de rastreamento do produto (opcional).</param>
    /// <returns>Retorna o status do produto, incluindo informações detalhadas.</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///     GET /v1/operation/checkproduct?serial=123456789
    ///     {
    ///       "uuid": "a3f1c96d-75b4-4b6a-baf3-61b91c478a9a",
    ///       "timelineUuid": "b2d7e56d-8f4d-4d6a-bfc3-62b71c478a9b",
    ///       "originUuid": "c3f8d87e-9f5e-5d7b-cgd4-63c82d589b0c",
    ///       "timelineStepUuid": "d4e9f98e-9f6e-6d8c-ehe5-74e93f67b2d4",
    ///       "productUuid": "e5f0g09f-9f7f-7e9d-fjf6-85f04g78c3e5",
    ///       "subProductUuid": "f6g1h10g-9f8g-8f0e-gkf7-96g15h89d4f6",
    ///       "currentStorageUuid": "g7h2i21h-9f9h-9g1f-hlg8-07h26i90e5g7",
    ///       "currentStatusUuid": "h8i3j32i-9g0i-0h2g-imh9-18i37j01f6h8",
    ///       "serialNumber": "123456789",
    ///       "trackingCode": "987654321",
    ///       "stepName": "Nome do Passo",
    ///       "stepInstruction": "Instruções do Passo",
    ///       "productName": "Nome do Produto",
    ///       "urlImage": "https://example.com/image.png",
    ///       "currentStorageName": "Nome do Armazenamento Atual",
    ///       "originName": "Nome da Origem",
    ///       "updatedAt": "2024-01-01T12:00:00.000000",
    ///       "defect": "Descrição do Defeito"
    ///     }
    ///
    /// </remarks>
    /// <response code="200">Retorna o status do produto com sucesso.</response>
    /// <response code="400">Erro ao buscar produto.</response>
    /// <response code="404">Produto não encontrado.</response>
    /// <response code="500">Retorna uma mensagem de erro interna do servidor.</response>

    [HttpGet]
    [Route("v1/operation/checkproduct")]
    public async Task<IActionResult> GetStatusProduct(string? serial, string? trackingcode)
    {

        await Task.Delay(0);

        OperationProductTrackingDTO productTrackingDTO = new();

        using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

        connection.Open();

        try
        {

            if (serial != null)
            {

                NpgsqlCommand command = new(@$"SELECT 
                                                    producttracking.trackinguuid,
                                                    producttracking.timelineuuid,
                                                    producttracking.originuuid,
                                                    producttracking.timelinestepuuid,
                                                    producttracking.productuuid,
                                                    producttracking.subproductuuid,
                                                    producttracking.storageuuid,
                                                    producttracking.statusuuid,
                                                    producttracking.serialnumber,
                                                    producttracking.trackingcode,
                                                    producttracking.updatedat,
                                                    timelinesteps.name as stepname,
                                                    timelinesteps.instructions,
                                                    products.name,
                                                    products.urlimage,
                                                    products.partnumber,
                                                    origins.name as originname,
                                                    storages.name as currentstoragename
                                                FROM producttracking
                                                INNER JOIN timelinesteps ON producttracking.timelinestepuuid = timelinesteps.uuid
                                                INNER JOIN products ON producttracking.productuuid = products.uuid
                                                INNER JOIN storages ON producttracking.storageuuid = storages.uuid
                                                INNER JOIN origins ON producttracking.originuuid = origins.uuid
                                                WHERE producttracking.serialnumber = '{serial}';
                                                ", connection);

                NpgsqlDataReader reader = command.ExecuteReader();

                if (reader.HasRows == true)
                {

                    if (reader.Read())
                    {
                        productTrackingDTO.Uuid = reader.GetGuid(reader.GetOrdinal("trackinguuid"));
                        productTrackingDTO.TimelineUuid = reader.GetGuid(reader.GetOrdinal("timelineuuid"));
                        productTrackingDTO.OriginUuid = reader.GetGuid(reader.GetOrdinal("originuuid"));
                        productTrackingDTO.TimelineStepUuid = reader.GetGuid(reader.GetOrdinal("timelinestepuuid"));
                        productTrackingDTO.ProductUuid = reader.IsDBNull(reader.GetOrdinal("productuuid")) ? null : reader.GetGuid(reader.GetOrdinal("productuuid"));
                        productTrackingDTO.SubProductUuid = reader.IsDBNull(reader.GetOrdinal("subproductuuid")) ? null : reader.GetGuid(reader.GetOrdinal("subproductuuid"));
                        productTrackingDTO.CurrentStorageUuid = reader.GetGuid(reader.GetOrdinal("storageuuid"));
                        productTrackingDTO.CurrentStatusUuid = reader.GetGuid(reader.GetOrdinal("statusuuid"));
                        productTrackingDTO.SerialNumber = reader.GetString(reader.GetOrdinal("serialnumber"));
                        productTrackingDTO.TrackingCode = reader.GetString(reader.GetOrdinal("trackingcode"));
                        productTrackingDTO.StepName = reader.GetString(reader.GetOrdinal("stepname"));
                        productTrackingDTO.StepInstruction = reader.GetString(reader.GetOrdinal("instructions"));
                        productTrackingDTO.ProductName = reader.GetString(reader.GetOrdinal("name"));
                        productTrackingDTO.UrlImage = reader.GetString(reader.GetOrdinal("urlimage"));
                        productTrackingDTO.CurrentStorageName = reader.GetString(reader.GetOrdinal("currentstoragename"));
                        productTrackingDTO.OriginName = reader.GetString(reader.GetOrdinal("originname"));
                        productTrackingDTO.UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"));
                        productTrackingDTO.PartNumber = reader.GetString(reader.GetOrdinal("partnumber"));

                    }

                    reader.Close();

                    NpgsqlCommand command2 = new(@$"SELECT event FROM kabumevents WHERE event::text LIKE '%{serial}%'", connection);

                    NpgsqlDataReader reader2 = command2.ExecuteReader();

                    if (reader2.Read())
                    {

                        EventDetails evento = new();

                        evento = JsonSerializer.Deserialize<EventDetails>(reader2.GetString(0))!;

                        productTrackingDTO.Defect = evento.Product!.Defect;
                        productTrackingDTO.OsNumber = evento.ServiceOrderNumber;
                    }

                    reader2.Close();

                }

                reader.Close();

            }

            if (trackingcode != null)
            {

                NpgsqlCommand command = new(@$"SELECT 
                                                    producttracking.trackinguuid,
                                                    producttracking.timelineuuid,
                                                    producttracking.originuuid,
                                                    producttracking.timelinestepuuid,
                                                    producttracking.productuuid,
                                                    producttracking.subproductuuid,
                                                    producttracking.storageuuid,
                                                    producttracking.statusuuid,
                                                    producttracking.serialnumber,
                                                    producttracking.trackingcode,
                                                    producttracking.updatedat,
                                                    timelinesteps.name as stepname,
                                                    timelinesteps.instructions,
                                                    products.name,
                                                    products.urlimage,
                                                    products.partnumber,
                                                    origins.name as originname,
                                                    storages.name as currentstoragename
                                                FROM producttracking
                                                INNER JOIN timelinesteps ON producttracking.timelinestepuuid = timelinesteps.uuid
                                                INNER JOIN products ON producttracking.productuuid = products.uuid
                                                INNER JOIN storages ON producttracking.storageuuid = storages.uuid
                                                INNER JOIN origins ON producttracking.originuuid = origins.uuid
                                                WHERE producttracking.trackingcode = '{trackingcode}';
                                                ", connection);

                NpgsqlDataReader reader = command.ExecuteReader();

                if (reader.HasRows == true)
                {

                    if (reader.Read())
                    {
                        productTrackingDTO.Uuid = reader.GetGuid(reader.GetOrdinal("trackinguuid"));
                        productTrackingDTO.TimelineUuid = reader.GetGuid(reader.GetOrdinal("timelineuuid"));
                        productTrackingDTO.OriginUuid = reader.GetGuid(reader.GetOrdinal("originuuid"));
                        productTrackingDTO.TimelineStepUuid = reader.GetGuid(reader.GetOrdinal("timelinestepuuid"));
                        productTrackingDTO.ProductUuid = reader.IsDBNull(reader.GetOrdinal("productuuid")) ? null : reader.GetGuid(reader.GetOrdinal("productuuid"));
                        productTrackingDTO.SubProductUuid = reader.IsDBNull(reader.GetOrdinal("subproductuuid")) ? null : reader.GetGuid(reader.GetOrdinal("subproductuuid"));
                        productTrackingDTO.CurrentStorageUuid = reader.GetGuid(reader.GetOrdinal("storageuuid"));
                        productTrackingDTO.CurrentStatusUuid = reader.GetGuid(reader.GetOrdinal("statusuuid"));
                        productTrackingDTO.SerialNumber = reader.GetString(reader.GetOrdinal("serialnumber"));
                        productTrackingDTO.TrackingCode = reader.GetString(reader.GetOrdinal("trackingcode"));
                        productTrackingDTO.StepName = reader.GetString(reader.GetOrdinal("stepname"));
                        productTrackingDTO.StepInstruction = reader.GetString(reader.GetOrdinal("instructions"));
                        productTrackingDTO.ProductName = reader.GetString(reader.GetOrdinal("name"));
                        productTrackingDTO.UrlImage = reader.GetString(reader.GetOrdinal("urlimage"));
                        productTrackingDTO.CurrentStorageName = reader.GetString(reader.GetOrdinal("currentstoragename"));
                        productTrackingDTO.OriginName = reader.GetString(reader.GetOrdinal("originname"));
                        productTrackingDTO.UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"));
                        productTrackingDTO.PartNumber = reader.GetString(reader.GetOrdinal("partnumber"));

                    }

                    reader.Close();

                    NpgsqlCommand command2 = new(@$"SELECT event FROM kabumevents WHERE event::text LIKE '%{trackingcode}%'", connection);

                    NpgsqlDataReader reader2 = command2.ExecuteReader();

                    if (reader2.Read())
                    {

                        EventDetails evento = new();

                        evento = JsonSerializer.Deserialize<EventDetails>(reader2.GetString(0))!;

                        productTrackingDTO.Defect = evento.Product!.Defect;
                        productTrackingDTO.OsNumber = evento.ServiceOrderNumber;
                    }

                    reader2.Close();

                }

                reader.Close();
            }

            if (productTrackingDTO.Uuid != null)
            {

                NpgsqlCommand command = new("UPDATE producttracking SET lastscanned = @Now WHERE trackinguuid = @Uuid;", connection);

                command.Parameters.AddWithValue("@Now", DateTime.Now.AddHours(-3));
                command.Parameters.AddWithValue("@Uuid", productTrackingDTO.Uuid);

                command.ExecuteNonQuery();

                return Ok(productTrackingDTO);
            }

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return BadRequest("Erro ao buscar produto.");
        }

        Log.Information("Produto não encontrado.");

        return NotFound("Produto não encontrado.");

    }

    /// <summary>
    /// Altera o status de um produto com base no número de série.
    /// </summary>
    /// <param name="steprelationuuid">UUID da relação do passo da linha do tempo.</param>
    /// <param name="notes">Notas sobre a mudança de status.</param>
    /// <param name="tenantUuid">Informar o cliente.</param>
    /// <param name="serial">Número de série do produto (opcional).</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro.</returns>
    /// <remarks>
    /// Exemplo de entrada:
    ///
    ///     POST /v1/operation/changestatus
    ///     {
    ///       "steprelationuuid": "123e4567-e89b-12d3-a456-426614174000",
    ///       "notes": "Produto movido para o próximo passo",
    ///       "serial": "SN1234567890"
    ///     }
    ///
    /// </remarks>
    /// <response code="200">Status do produto alterado com sucesso.</response>
    /// <response code="400">Erro ao alterar status do produto.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpPost]
    [Route("v1/operation/changestatus")]
    public async Task<IActionResult> ChangeStatusProduct(string steprelationuuid, string notes, string? serial)
    {
        await Task.Delay(0);

        HttpResponseMessage response = new();

        OperationProductTrackingDTO productTrackingDTO = new();

        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using NpgsqlCommand cmd = new(@$"SELECT * FROM producttracking WHERE serialnumber = '{serial}'", connection);

            NpgsqlDataReader reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                productTrackingDTO.Uuid = reader.GetGuid(reader.GetOrdinal("trackinguuid"));
                productTrackingDTO.OriginUuid = reader.GetGuid(reader.GetOrdinal("originuuid"));
                productTrackingDTO.TimelineUuid = reader.GetGuid(reader.GetOrdinal("timelineuuid"));
                productTrackingDTO.TimelineStepUuid = reader.GetGuid(reader.GetOrdinal("timelinestepuuid"));
                productTrackingDTO.ProductUuid = reader.IsDBNull(reader.GetOrdinal("productuuid")) ? null : reader.GetGuid(reader.GetOrdinal("productuuid"));
                productTrackingDTO.SubProductUuid = reader.IsDBNull(reader.GetOrdinal("subproductuuid")) ? null : reader.GetGuid(reader.GetOrdinal("subproductuuid"));
                productTrackingDTO.CurrentStorageUuid = reader.GetGuid(reader.GetOrdinal("storageuuid"));
                productTrackingDTO.CurrentStatusUuid = reader.GetGuid(reader.GetOrdinal("statusuuid"));
                productTrackingDTO.SerialNumber = reader.GetString(reader.GetOrdinal("serialnumber"));
                productTrackingDTO.TrackingCode = reader.GetString(reader.GetOrdinal("trackingcode"));
                productTrackingDTO.TenantUuid = reader.GetGuid(reader.GetOrdinal("tenantuuid"));
            }

            reader.Close();

            using NpgsqlCommand cmd2 = new(@$"SELECT * FROM timelinestepsrelationship WHERE uuid = '{steprelationuuid}'", connection);

            NpgsqlDataReader reader2 = cmd2.ExecuteReader();

            if (reader2.Read())
            {

                productTrackingDTO.NextTimelineStepUuid = reader2.GetGuid(reader2.GetOrdinal("tosteplineuuid"));
                productTrackingDTO.NextStatusUuid = reader2.GetGuid(reader2.GetOrdinal("tostatusuuid"));
                productTrackingDTO.NextStorageUuid = reader2.GetGuid(reader2.GetOrdinal("tostorageuuid"));
            }

            reader2.Close();

            if (productTrackingDTO.OriginUuid == Guid.Parse("7adeb01e-d8fa-4089-9bd0-3e4f6dc21f2b"))
            {

                if (productTrackingDTO.SerialNumber != null)
                {

                    response = await KabumService.ChangeStatus(productTrackingDTO.SerialNumber, null, productTrackingDTO.NextStatusUuid);

                }
                else
                {

                    response = await KabumService.ChangeStatus(null, productTrackingDTO.TrackingCode, productTrackingDTO.NextStatusUuid);
                }

            }

            Log.Information("Resposta da API Kabum: {0}", response.StatusCode);

            if (response.StatusCode != HttpStatusCode.OK)
            {

                return BadRequest("Erro ao alterar status do produto.");
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {

                NpgsqlCommand command = new(@$"UPDATE producttracking
                                                SET statusuuid = @NextStatusUuid, updatedat = @Now, storageuuid = @NextStorageUuid, timelinestepuuid = @NextTimelineStepUuid
                                                WHERE trackinguuid = @Uuid;
                                                ", connection);

                command.Parameters.AddWithValue("@NextTimelineStepUuid", productTrackingDTO.NextTimelineStepUuid!);
                command.Parameters.AddWithValue("@NextStatusUuid", productTrackingDTO.NextStatusUuid!);
                command.Parameters.AddWithValue("@NextStorageUuid", productTrackingDTO.NextStorageUuid!);
                command.Parameters.AddWithValue("@Uuid", productTrackingDTO.Uuid!);
                command.Parameters.AddWithValue("@Now", DateTime.Now.AddHours(-3));

                command.ExecuteNonQuery();

                NpgsqlCommand command2 = new("INSERT INTO productshistory (uuid,tenantuuid, timelineuuid, originuuid, productuuid, subproductuuid, fromstorageuuid, tostorageuuid, fromstatusuuid, tostatusuuid, serialnumber, trackingcode, notes, createdat) VALUES (@Uuid,@tenantUuid, @TimelineUuid, @OriginUuid, @ProductUuid, @SubProductUuid, @FromStorageUuid, @ToStorageUuid, @FromStatusUuid, @ToStatusUuid, @SerialNumber, @TrackingCode, @Notes, @Now);", connection);

                command2.Parameters.AddWithValue("@Uuid", Guid.NewGuid());
                command2.Parameters.AddWithValue("@tenantuuid", productTrackingDTO.TenantUuid! == null ? DBNull.Value : productTrackingDTO.TenantUuid);
                command2.Parameters.AddWithValue("@TimelineUuid", productTrackingDTO.TimelineUuid!);
                command2.Parameters.AddWithValue("@OriginUuid", productTrackingDTO.OriginUuid!);
                command2.Parameters.AddWithValue("@ProductUuid", productTrackingDTO.ProductUuid == null ? DBNull.Value : productTrackingDTO.ProductUuid);
                command2.Parameters.AddWithValue("@SubProductUuid", productTrackingDTO.SubProductUuid == null ? DBNull.Value : productTrackingDTO.SubProductUuid);
                command2.Parameters.AddWithValue("@FromStorageUuid", productTrackingDTO.CurrentStorageUuid!);
                command2.Parameters.AddWithValue("@ToStorageUuid", productTrackingDTO.NextStorageUuid!);
                command2.Parameters.AddWithValue("@FromStatusUuid", productTrackingDTO.CurrentStatusUuid!);
                command2.Parameters.AddWithValue("@ToStatusUuid", productTrackingDTO.NextStatusUuid!);
                command2.Parameters.AddWithValue("@SerialNumber", productTrackingDTO.SerialNumber!);
                command2.Parameters.AddWithValue("@TrackingCode", productTrackingDTO.TrackingCode!);
                command2.Parameters.AddWithValue("@Notes", notes);
                command2.Parameters.AddWithValue("@Now", DateTime.Now.AddHours(-3));

                command2.ExecuteNonQuery();
            }

            return Ok("Status do produto alterado com sucesso.");

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return BadRequest("Erro ao alterar status do produto.");
        }

    }

    /// <summary>
    /// Retorna os possíveis próximos passos de um produto na linha do tempo com base no UUID do passo atual.
    /// </summary>
    /// <param name="timelinestepuuid">UUID do passo atual da linha do tempo.</param>
    /// <returns>Retorna uma lista de possíveis próximos passos com os respectivos UUIDs e nomes.</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///     GET /v1/operation/checkproductfork?timelinestepuuid=123e4567-e89b-12d3-a456-426614174000
    ///     [
    ///       {
    ///         "relationUuid": "789e0123-e89b-12d3-a456-426614174000",
    ///         "toStepName": "Próximo Passo 1",
    ///         "toStepTimelineUuid": "456e7890-e89b-12d3-a456-426614174000"
    ///       },
    ///       {
    ///         "relationUuid": "890e1234-e89b-12d3-a456-426614174000",
    ///         "toStepName": "Próximo Passo 2",
    ///         "toStepTimelineUuid": "567e8901-e89b-12d3-a456-426614174000"
    ///       }
    ///     ]
    ///
    /// </remarks>
    /// <response code="200">Retorna uma lista de possíveis próximos passos.</response>
    /// <response code="400">Erro ao buscar passos do produto.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/operation/checkproductfork")]
    public async Task<IActionResult> GetProductFork(string timelinestepuuid)
    {

        await Task.Delay(0);

        using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

        List<Dictionary<string, string>> forkSteps = [];

        connection.Open();

        try
        {

            NpgsqlCommand commandnext = new(@$"SELECT r.uuid, t.name, r.tosteplineuuid
                                                FROM timelinesteps t
                                                JOIN timelinestepsrelationship r ON t.uuid = r.tosteplineuuid
                                                WHERE r.fromsteplineuuid ='{timelinestepuuid}'", connection);

            NpgsqlDataReader readernext = commandnext.ExecuteReader();

            while (readernext.Read())
            {

                Dictionary<string, string> status = new()
                {
                    { "relationUuid", readernext.GetGuid(readernext.GetOrdinal("uuid")).ToString()},
                    { "toStepName", readernext.GetString(readernext.GetOrdinal("name")) },
                    { "toStepTimelineUuid", readernext.GetGuid(readernext.GetOrdinal("tosteplineuuid")).ToString() }
                };

                forkSteps.Add(status);

            }

            readernext.Close();


            return Ok(forkSteps);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return BadRequest("Erro ao buscar passos do produto.");
        }
    }

    /// <summary>
    /// Retorna o código de postagem associado a um número de série ou código de rastreamento.
    /// </summary>
    /// <param name="serial">Número de série do produto (opcional).</param>
    /// <param name="trackingcode">Código de rastreamento do produto (opcional).</param>
    /// <returns>Retorna o código de postagem se encontrado.</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///     GET /v1/operation/getrepostcode?trackingcode=987654321
    ///     "ABC123DEF"
    ///
    /// </remarks>
    /// <response code="200">Retorna o código de postagem com sucesso.</response>
    /// <response code="404">Código de postagem não encontrado.</response>
    /// <response code="400">Erro ao buscar código de postagem.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/operation/getrepostcode")]
    public async Task<IActionResult> GetRepostCode(string? serial, string? trackingcode)
    {
        await Task.Delay(0);

        using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

        connection.Open();

        try
        {

            NpgsqlCommand command = new(@$"SELECT event FROM kabumevents WHERE event::text LIKE '%{trackingcode}%'", connection);

            NpgsqlDataReader reader = command.ExecuteReader();

            if (reader.Read())
            {

                EventDetails evento = new();

                evento = JsonSerializer.Deserialize<EventDetails>(reader.GetString(0))!;

                return Ok(evento.AuthorizationCode);
            }

            return NotFound("Código de postagem não encontrado.");

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return BadRequest("Erro ao buscar código de postagem.");
        }
    }

    [HttpGet]
    [Route("v1/operation/getsteps")]
    public async Task<IActionResult> GetSteps(string serial)
    {
        await Task.Delay(0);

        using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

        connection.Open();

        using NpgsqlConnection connection2 = new(EnvInterface.SQLPostgres);

        connection2.Open();

        List<OperationStepStepperDTO> steps = [];

        Guid? fromStatusUuid = null;
        Guid? toStatusUuid = null;
        Guid? toStepUuid = null;

        try
        {

            NpgsqlCommand command = new("SELECT * FROM productshistory WHERE serialnumber = @serial ORDER BY createdat", connection);

            command.Parameters.AddWithValue("@serial", serial);

            NpgsqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {

                fromStatusUuid = reader.IsDBNull(reader.GetOrdinal("fromstatusuuid")) ? null : reader.GetGuid(reader.GetOrdinal("fromstatusuuid"));
                toStatusUuid = reader.GetGuid(reader.GetOrdinal("tostatusuuid"));

                OperationStepStepperDTO step = new()
                {
                    Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? "sem notas" : reader.GetString(reader.GetOrdinal("notes")),
                    Date = reader.GetDateTime(reader.GetOrdinal("createdat")).ToString("dd/MM/yyyy HH:mm:ss")
                };

                StringBuilder query = new("SELECT * FROM timelinestepsrelationship WHERE");

                bool appendComma = false;

                void AppendCommaIfRequired()
                {
                    if (appendComma) query.Append(", ");
                    appendComma = true;
                }

                if (toStatusUuid != null && fromStatusUuid != null) { query.Append(" tostatusuuid = @toStatusUuid"); }
                if (toStatusUuid != null && fromStatusUuid == null) { query.Append(" fromstatusuuid = @toStatusUuid"); }
                if (fromStatusUuid != null && toStatusUuid == null) { AppendCommaIfRequired(); query.Append(" fromstatusuuid = @fromStatusUuid"); }
                if (fromStatusUuid != null && toStatusUuid != null) { AppendCommaIfRequired(); query.Append(" AND fromstatusuuid = @fromStatusUuid"); }

                NpgsqlCommand command2 = new(query.ToString(), connection2);

                if (toStatusUuid != null) { command2.Parameters.AddWithValue("@toStatusUuid", toStatusUuid); }
                if (fromStatusUuid != null) { command2.Parameters.AddWithValue("@fromStatusUuid", fromStatusUuid); }

                NpgsqlDataReader reader2 = command2.ExecuteReader();

                if (reader2.Read())
                {

                    if (steps.Count == 0)
                    {

                        toStepUuid = reader2.GetGuid(reader2.GetOrdinal("fromsteplineuuid"));

                    }
                    else
                    {

                        toStepUuid = reader2.GetGuid(reader2.GetOrdinal("tosteplineuuid"));

                    }

                }

                reader2.Close();

                NpgsqlCommand command3 = new(@$"SELECT * FROM timelinesteps WHERE uuid = @toStepUuid", connection2);

                command3.Parameters.AddWithValue("@toStepUuid", toStepUuid!);

                NpgsqlDataReader reader3 = command3.ExecuteReader();

                if (reader3.Read())
                {

                    step.StepName = reader3.GetString(reader3.GetOrdinal("name"));

                    steps.Add(step);
                }

                reader3.Close();

                fromStatusUuid = null;
                toStatusUuid = null;
                toStepUuid = null;

            }

            reader.Close();

            return Ok(steps);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return BadRequest("Erro ao buscar código de postagem.");
        }
    }


}
