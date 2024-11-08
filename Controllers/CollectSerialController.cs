using Microsoft.AspNetCore.Mvc;
using rmaesolutions.configInterface;
using Npgsql;
using Serilog;
using System.Net;
using rmaesolutions.dto;

namespace rmaesolutions.Controllers;

[ApiController]
public class CollectSerialController : ControllerBase
{
    /// <summary>
    /// Retorna uma Categoria.
    /// </summary>
    /// <param name="collectSerialDTOs">UUID da Serial</param>
    /// <returns>Retorna uma Categoria</returns>
    /// <response code="200">Retorna uma Categoria</response>
    /// <response code="500">Retorna uma mensagem de erro</response>

    [HttpPost]
    [Route("v1/collectserial/insert")]
    public async Task<IActionResult> Save([FromBody] List<CollectSerialDTO> collectSerialDTOs)
    {
        try
        {
            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            await connection.OpenAsync();

            foreach (var collectSerialDTO in collectSerialDTOs)
            {
                using NpgsqlCommand command = new("INSERT INTO collectserial (uuid, tenantuuid, serialnumber, purchase, sku, ean, createdat) VALUES (@uuid,@tenantuuid, @serialnumber, @purchase, @sku, @ean,  @createdat)", connection);

                command.Parameters.AddWithValue("@uuid", Guid.NewGuid());
                command.Parameters.AddWithValue("@tenantuuid", collectSerialDTO.TenantUuid!).NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Uuid;
                command.Parameters.AddWithValue("@serialnumber", collectSerialDTO.SerialNumber!);
                command.Parameters.AddWithValue("@purchase", collectSerialDTO.Purchase!);
                command.Parameters.AddWithValue("@sku", collectSerialDTO.SKU ?? "");
                command.Parameters.AddWithValue("@ean", collectSerialDTO.EAN ?? "");
                command.Parameters.AddWithValue("@createdat", DateTime.Now.AddHours(-3));

                await command.ExecuteNonQueryAsync(); // comando executado de forma assíncrona
            }

            return Ok("Serials saved successfully");
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }
    }


    /// <summary>
    /// Retorna uma lista de seriais coletados dentro de um intervalo de datas e opcionalmente filtrados por grupo de empresa.
    /// </summary>
    /// <param name="datefrom">Data de início do intervalo.</param>
    /// <param name="dateto">Data de fim do intervalo.</param>
    /// <param name="tenantuuid">filtrar por cliente.</param>
    /// <returns>Retorna uma lista de seriais coletados.</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///    [
    ///     {
    ///      "uuid": "62e37d39-5b56-457d-ab79-09ec1c490dd1",
    ///      "serialNumber": "123456789",
    ///      "purchase": "PurchaseOrder123",
    ///      "sku": "SKU12345",
    ///      "ean": "EAN1234567890123",
    ///      "createdAt": "2024-06-25T15:14:58.729747"
    ///     },
    ///     {
    ///      "uuid": "22f736b2-876c-4c68-953c-bde97ee4e0a3",
    ///      "serialNumber": "987654321",
    ///      "purchase": "PurchaseOrder456",
    ///      "sku": "SKU67890",
    ///      "ean": "EAN9876543210987",
    ///      "createdAt": "2024-06-26T16:19:34.47457"
    ///     }
    ///    ]
    ///
    /// </remarks>
    /// <response code="200">Retorna uma lista de seriais coletados.</response>
    /// <response code="400">Se os parâmetros de data forem inválidos.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/collectserial/get")]
    public IActionResult getSerial(DateTime datefrom, DateTime dateto, string tenantuuid)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            NpgsqlCommand command = new("SELECT * FROM collectserial WHERE createdat BETWEEN @datefrom AND @dateto AND tenantuuid = @tenantuuid", connection);

            command.Parameters.AddWithValue("@datefrom", datefrom);
            command.Parameters.AddWithValue("@dateto", dateto);
            command.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));

            NpgsqlDataReader reader = command.ExecuteReader();

            List<Serial> seriais = [];

            while (reader.Read())
            {
                Serial serial = new()
                {
                    Uuid = Guid.Parse(reader["uuid"].ToString()!),
                    SerialNumber = reader["serialnumber"].ToString(),
                    Purchase = reader["purchase"].ToString(),
                    SKU = reader["sku"].ToString(),
                    EAN = reader["ean"].ToString(),
                    CreatedAt = Convert.ToDateTime(reader["createdat"])
                };

                seriais.Add(serial);
            }

            return Ok(seriais);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }

    /// <summary>
    /// Obtém a lista de números seriais por número de compra.
    /// </summary>
    /// <param name="purchase">O número de compra para consulta.</param>
    /// <param name="tenantuuid">filtrar por cliente.</param>
    /// <returns>Retorna uma lista de objetos PurchasetDTO contendo informações dos números seriais agrupados por produtos.</returns>
    /// <remarks>
    /// Exemplo de retorno:
    /// 
    /// [
    ///     {
    ///         "ProductName": "ProductA",
    ///         "Count": 2,
    ///         "EAN": "111111",
    ///         "SerialNumbers": [
    ///             "123456",
    ///             "123457"
    ///         ]
    ///     },
    ///     {
    ///         "ProductName": "ProductB",
    ///         "Count": 2,
    ///         "EAN": "222222",
    ///         "SerialNumbers": [
    ///             "123458",
    ///             "123459"
    ///         ]
    ///     }
    /// ]
    ///
    /// </remarks>
    /// <response code="200">Retorna a lista de números seriais agrupados por produto.</response>
    /// <response code="400">Se o parâmetro purchase não for fornecido.</response>
    /// <response code="500">Erro interno. Verifique os logs para mais detalhes.</response>
    [HttpGet]
    [Route("v1/collectserial/purchaseorders/serials")]
    public IActionResult GetOrderByPurchase(string purchase, string tenantuuid)
    {
        if (string.IsNullOrWhiteSpace(purchase))
        {
            return BadRequest("Forneça o número de compra para consulta.");
        }

        try
        {
            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);
            connection.Open();

            string query =
            @"SELECT COUNT(collectserial.serialnumber) AS COUNT, collectserial.ean, 
                 STRING_AGG(collectserial.serialnumber, ', ') AS serialnumbers, products.name
          FROM collectserial
          INNER JOIN products ON collectserial.ean = products.barcode
          WHERE collectserial.purchase = @purchase AND collectserial.tenantuuid = @tenantuuid
          GROUP BY collectserial.ean, products.name;";

            NpgsqlCommand command = new(query, connection);

            command.Parameters.AddWithValue("@purchase", purchase);
            command.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));

            NpgsqlDataReader reader = command.ExecuteReader();

            List<PurchasetDTO> serials = new();  // Inicializa corretamente a lista

            while (reader.Read())
            {
                PurchasetDTO serial = new()
                {
                    ProductName = reader["name"].ToString(),
                    Count = Convert.ToInt32(reader["COUNT"]),
                    EAN = reader["ean"].ToString(),
                    SerialNumbers = reader["serialnumbers"].ToString()!.Split(',').Select(s => s.Trim()).ToList()
                };

                serials.Add(serial);
            }

            return Ok(serials);
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }
    }


    /// <summary>
    /// Obtém a lista de pedidos que contém o SKU e opcionalmente por grupo de empresa.
    /// </summary>
    /// <param name="sku">O SKU do produto para consulta.</param>
    /// <param name="tenantuuid">Filtrar por cliente.</param>
    /// <returns>Retorna uma lista de pedidos que contém o SKU.</returns>
    /// <remarks> 
    /// Exemplo de retorno:
    /// [
    ///     {
    ///         "Count": 2,
    ///         "EAN": "111111",
    ///         "Purchase": "2023-07-01",
    ///         "ProductName": "ProductA"
    ///     },
    ///     {
    ///         "Count": 3,
    ///         "EAN": "222222",
    ///         "Purchase": "2023-06-30",
    ///         "ProductName": "ProductB"
    ///     }
    /// ]
    /// </remarks>
    /// <response code="200">Retorna a lista de números seriais.</response>
    /// <response code="400">Se o parâmetro SKU não for fornecido.</response>
    /// <response code="500">Erro interno. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/collectserial/purchaseorders/bysku")]
    public IActionResult GetOrderBySku(string sku, string tenantuuid)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return BadRequest("Forneça um SKU para consulta.");
        }

        try
        {
            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);
            connection.Open();

            string query = @"
            SELECT COUNT(c.serialnumber) AS Count, c.ean, c.purchase, p.name
            FROM collectserial c
            INNER JOIN products p ON c.sku = ANY(p.skus)
            WHERE c.sku = @sku AND p.tenantuuid = @tenantuuid
            GROUP BY c.ean, c.purchase, p.name
            ORDER BY c.purchase DESC;";

            using NpgsqlCommand command = new(query, connection);

            command.Parameters.AddWithValue("@sku", sku);
            command.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));

            using NpgsqlDataReader reader = command.ExecuteReader();

            List<PurchasetDTO> purchases = new();

            while (reader.Read())
            {
                PurchasetDTO purchase = new()
                {
                    Count = Convert.ToInt32(reader["Count"]),
                    Purchase = reader["purchase"].ToString(),
                    EAN = reader["ean"].ToString(),
                    ProductName = reader["name"].ToString()
                };

                purchases.Add(purchase);
            }

            return Ok(purchases);
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return StatusCode(StatusCodes.Status500InternalServerError, "Erro interno. Verifique os logs para mais detalhes.");
        }
    }


    /// <summary>
    /// Retorna a lista com a quantidade de itens agrupados por pedidos a partir de um EAN.
    /// </summary>
    /// <param name="ean">O EAN do produto para consulta.</param>
    /// <param name="tenantuuid">filtrar por cliente.</param>
    /// <returns>Retorna uma lista de pedidos e a quantidade do item em cada um deles.</returns>
    /// <remarks>
    /// 
    /// Exemplo de retorno:
    /// 
    /// [
    ///     {
    ///         "Count": 2,
    ///         "EAN": "111111",
    ///         "Purchase": "01",
    ///         "ProductName": "ProductA"
    ///     },
    ///     {
    ///         "Count": 3,
    ///         "EAN": "111111",
    ///         "Purchase": "02",
    ///         "ProductName": "ProductA"
    ///     }
    /// ]
    /// 
    /// </remarks>
    /// <response code="200">Retorna a lista de números seriais.</response>
    /// <response code="400">Se o parâmetro EAN não for fornecido.</response>
    /// <response code="500">Erro interno. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/collectserial/purchaseorders/byean")]
    public IActionResult GetOrderByEAN(string ean, string tenantuuid)
    {
        if (string.IsNullOrWhiteSpace(ean))
        {
            return BadRequest("Forneça um ean para consulta.");
        }

        try
        {
            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);
            connection.Open();

            string query =
                @"SELECT COUNT(serialnumber) as COUNT, ean, purchase, products.name
            FROM collectserial
            INNER JOIN products ON ean = products.barcode
            WHERE collectserial.ean = @ean AND collectserial.tenantuuid = @tenantuuid
            GROUP BY purchase, ean, products.name
            ORDER BY purchase DESC;";

            NpgsqlCommand command = new(query, connection);

            command.Parameters.AddWithValue("@ean", ean);
            command.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));

            NpgsqlDataReader reader = command.ExecuteReader();

            List<PurchasetDTO> purchases = new();  // Inicializa corretamente a lista

            while (reader.Read())
            {
                PurchasetDTO purchase = new()
                {
                    Count = Convert.ToInt32(reader["COUNT"]),
                    Purchase = reader["purchase"].ToString(),
                    EAN = reader["ean"].ToString(),
                    ProductName = reader["name"].ToString()
                };

                purchases.Add(purchase);
            }

            return Ok(purchases);
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }
    }


}

