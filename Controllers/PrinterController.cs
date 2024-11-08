using Microsoft.AspNetCore.Mvc;
using rmaesolutions.configInterface;
using Npgsql;
using Serilog;
using rmaesolutions.entities;
using System.Net;
using System.Text;
using rmaesolutions.dto;
using Newtonsoft.Json;

namespace rmaesolutions.Controllers;

[ApiController]
public class PrinterController : ControllerBase
{
    /// <summary>
    /// Retorna todas as impressoras cadastradas.
    /// </summary>
    /// <returns>Retorna uma lista de impressoras.</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///     GET /v1/printers/getall
    ///     [
    ///       {
    ///         "uuid": "123e4567-e89b-12d3-a456-426614174000",
    ///         "name": "Printer 1",
    ///         "ip": "192.168.0.100",
    ///         "createdAt": "2024-01-01T12:00:00.000000",
    ///         "updatedAt": "2024-01-01T12:00:00.000000"
    ///       },
    ///       {
    ///         "uuid": "234e5678-e89b-12d3-a456-426614174000",
    ///         "name": "Printer 2",
    ///         "ip": "192.168.0.101",
    ///         "createdAt": "2024-01-01T12:00:00.000000",
    ///         "updatedAt": "2024-01-01T12:00:00.000000"
    ///       }
    ///     ]
    /// </remarks>
    /// <response code="200">Retorna uma lista de impressoras cadastradas.</response>
    /// <response code="204">Se a tabela de impressoras estiver vazia.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/printers/getall")]
    public async Task<dynamic> GetAllPrinters(string tenantuuid)
    {

        using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

        await connection.OpenAsync();
        try
        {

            using NpgsqlCommand command = new("SELECT * FROM printers WHERE tenantuuid = @tenantuuid", connection);

            command.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));


            using NpgsqlDataReader reader = command.ExecuteReader();

            if (!reader.HasRows)
            {
                return NotFound("Nenhuma impressora cadastrada!");
            }

            List<Printer> printers = [];

            while (reader.Read())
            {
                Printer printer = new()
                {
                    Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Ip = reader.GetString(reader.GetOrdinal("ip")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
                };

                printers.Add(printer);
            }

            return printers;
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return StatusCode(500, "Internal Server Error");
        }
    }

    /// <summary>
    /// Retorna uma impressora específica com base no UUID fornecido.
    /// </summary>
    /// <param name="uuid">UUID da impressora.</param>
    /// <returns>Retorna uma impressora.</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///     GET /v1/printers/get?uuid=123e4567-e89b-12d3-a456-426614174000
    ///     {
    ///       "uuid": "123e4567-e89b-12d3-a456-426614174000",
    ///       "name": "Printer 1",
    ///       "ip": "192.168.0.100",
    ///       "createdAt": "2024-01-01T12:00:00.000000",
    ///       "updatedAt": "2024-01-01T12:00:00.000000"
    ///     }
    ///
    /// </remarks>
    /// <response code="200">Retorna a impressora solicitada.</response>
    /// <response code="404">Impressora não encontrada.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/printers/get")]
    public async Task<dynamic> GetPrinter(Guid uuid)
    {

        using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

        await connection.OpenAsync();

        try
        {

            using NpgsqlCommand command = new("SELECT * FROM printers where uuid = @uuid", connection);

            command.Parameters.AddWithValue("@uuid", uuid);

            using NpgsqlDataReader reader = command.ExecuteReader();

            Printer printer = new();

            while (reader.Read())
            {
                printer = new()
                {
                    Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Ip = reader.GetString(reader.GetOrdinal("ip")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
                };
            }

            return printer;
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return StatusCode(500, "Internal Server Error");
        }
    }

    /// <summary>
    /// Cria ou atualiza uma impressora.
    /// </summary>
    /// <param name="printer">Objeto PrinterDTO contendo as informações da impressora.</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro.</returns>
    /// <remarks>
    /// Exemplo de entrada para criação:
    ///
    ///     POST /v1/printers/create
    ///     {
    ///       "name": "Printer 1",
    ///       "ip": "192.168.0.100"
    ///     }
    ///
    /// Exemplo de entrada para atualização:
    ///
    ///     POST /v1/printers/update
    ///     {
    ///       "uuid": "123e4567-e89b-12d3-a456-426614174000",
    ///       "name": "Updated Printer 1",
    ///       "ip": "192.168.0.101"
    ///     }
    ///
    /// </remarks>
    /// <response code="200">Impressora criada ou atualizada com sucesso.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpPost]
    [Route("v1/printers/create")]
    [Route("v1/printers/update")]
    public async Task<dynamic> UpsertPrinters([FromBody] PrinterDTO printer)
    {

        using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

        await connection.OpenAsync();

        try
        {

            if (printer.Uuid == null)
            {

                using NpgsqlCommand command = new("INSERT INTO printers (uuid,tenantuuid, name, ip, createdat, updatedat) VALUES (@uuid, @tenantuuid, @name, @ip, @createdat, @updatedat)", connection);

                command.Parameters.AddWithValue("@uuid", Guid.NewGuid());
                command.Parameters.AddWithValue("@tenantuuid", printer.TenantUuid!);
                command.Parameters.AddWithValue("@name", printer.Name!);
                command.Parameters.AddWithValue("@ip", printer.Ip!);
                command.Parameters.AddWithValue("@createdat", DateTime.Now.AddHours(-3));
                command.Parameters.AddWithValue("@updatedat", DateTime.Now.AddHours(-3));

                command.ExecuteNonQuery();

                return StatusCode(200, "Impressora criado com sucesso");
            }
            else
            {
                using NpgsqlCommand command = new("UPDATE printers SET name = @name, ip = @ip, updatedat = @updatedat WHERE uuid = @uuid", connection);

                command.Parameters.AddWithValue("@uuid", printer.Uuid);
                command.Parameters.AddWithValue("@name", printer.Name!);
                command.Parameters.AddWithValue("@ip", printer.Ip!);
                command.Parameters.AddWithValue("@updatedat", DateTime.Now.AddHours(-3));

                command.ExecuteNonQuery();

                return StatusCode(200, "Impressora atualizado com sucesso");
            }
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return StatusCode(500, "Internal Server Error");
        }

    }

    /// <summary>
    /// Imprime um número de série em uma etiqueta utilizando uma impressora específica.
    /// </summary>
    /// <param name="printeruuid">UUID da impressora.</param>
    /// <param name="labeluuid">UUID da etiqueta.</param>
    /// <param name="serial">Número de série a ser impresso.</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro.</returns>
    /// <remarks>
    /// Exemplo de entrada:
    ///
    ///     POST /v1/printers/printserial
    ///     {
    ///       "printeruuid": "123e4567-e89b-12d3-a456-426614174000",
    ///       "labeluuid": "234e5678-e89b-12d3-a456-426614174000",
    ///       "serial": "SN1234567890"
    ///     }
    ///
    /// </remarks>
    /// <response code="200">Impressão realizada com sucesso.</response>
    /// <response code="500">Erro ao realizar impressão. Verifique os logs para mais detalhes.</response>

    [HttpPost]
    [Route("v1/printers/printserial")]
    public async Task<dynamic> PrintSerial(Guid printeruuid, Guid labeluuid, string serial)
    {
        try
        {
            using HttpClient client = new();

            Printer printer = new();

            Label label = new();

            string productname = "";

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using NpgsqlCommand command = new("SELECT * FROM printers where uuid = @uuid", connection);

            command.Parameters.AddWithValue("@uuid", printeruuid);

            using NpgsqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                printer = new()
                {
                    Ip = reader.GetString(reader.GetOrdinal("ip")),
                };
            }

            reader.Close();

            using NpgsqlCommand command2 = new("SELECT * FROM labels where uuid = @uuid", connection);

            command2.Parameters.AddWithValue("@uuid", labeluuid);

            using NpgsqlDataReader reader2 = command2.ExecuteReader();

            while (reader2.Read())
            {
                label = new()
                {
                    ZPL = reader2.GetString(reader2.GetOrdinal("zpl")),
                };
            }

            reader2.Close();

            using NpgsqlCommand command3 = new(@"SELECT pt.*, p.name
                                                FROM producttracking pt
                                                JOIN products p ON pt.productuuid = p.uuid
                                                WHERE pt.serialnumber = @serial;
                                                ", connection);

            command3.Parameters.AddWithValue("@serial", serial);

            using NpgsqlDataReader reader3 = command3.ExecuteReader();

            while (reader3.Read())
            {
                productname = reader3.GetString(reader3.GetOrdinal("name"));
            }

            label.ZPL = label.ZPL!.Replace("${serial}", serial);
            label.ZPL = label.ZPL!.Replace("${productName.line1}", productname);
            label.ZPL = label.ZPL!.Replace("${productName.line2}", productname);

            HttpResponseMessage response = await client.PostAsync($"http://{printer.Ip}/pstprnt", new StringContent(label.ZPL!, Encoding.UTF8));

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return StatusCode(200, "Impressão realizada com sucesso");
            }
            else
            {
                return StatusCode(500, "Erro ao realizar impressão");
            }
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpPost]
    [Route("v1/printers/printrepostcode")]
    public async Task<dynamic> PrintRepostCode(Guid printeruuid, Guid labeluuid, string osnumber)
    {
        Label label = new();

        try
        {
            using HttpClient client = new();

            Printer printer = new();

            EventDetails RepostEvent = new();

            EventDetails OsEvent = new();

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using NpgsqlCommand command = new("SELECT * FROM printers where uuid = @uuid", connection);

            command.Parameters.AddWithValue("@uuid", printeruuid);

            using NpgsqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                printer = new()
                {
                    Ip = reader.GetString(reader.GetOrdinal("ip")),
                };
            }

            reader.Close();

            using NpgsqlCommand command2 = new("SELECT * FROM labels where uuid = @uuid", connection);

            command2.Parameters.AddWithValue("@uuid", labeluuid);

            using NpgsqlDataReader reader2 = command2.ExecuteReader();

            while (reader2.Read())
            {
                label = new()
                {
                    ZPL = reader2.GetString(reader2.GetOrdinal("zpl")),
                };
            }

            reader2.Close();

            using NpgsqlCommand command3 = new(@$"SELECT event FROM kabumevents where eventname = 'GeneratedPostAuthorization' and event->>'ServiceOrderNumber' = @osnumber", connection);

            command3.Parameters.AddWithValue("@osnumber", osnumber);

            using NpgsqlDataReader reader3 = command3.ExecuteReader();

            while (reader3.Read())
            {
                RepostEvent = JsonConvert.DeserializeObject<EventDetails>(reader3.GetString(reader3.GetOrdinal("event")))!;
            }

            reader3.Close();

            using NpgsqlCommand command4 = new(@$"SELECT event FROM kabumevents where eventname = 'NewServiceOrder' and event->>'ServiceOrderNumber' = @osnumber", connection);

            command4.Parameters.AddWithValue("@osnumber", osnumber);

            using NpgsqlDataReader reader4 = command4.ExecuteReader();

            while (reader4.Read())
            {
                OsEvent = JsonConvert.DeserializeObject<EventDetails>(reader4.GetString(reader4.GetOrdinal("event")))!;
            }

            reader4.Close();

            label.ZPL = label.ZPL!.Replace("${nomeDestinatario}", OsEvent.Customer!.Name!);
            label.ZPL = label.ZPL!.Replace("${cpfcpnjDestinatario}", OsEvent.Customer!.Document!.Cpf!);
            label.ZPL = label.ZPL!.Replace("${enderecoDestinatario}", OsEvent.Customer!.Address!.Street! + ", " + OsEvent.Customer!.Address!.Number! + " - " + OsEvent.Customer!.Address!.City! + " - " + OsEvent.Customer!.Address!.State!);
            label.ZPL = label.ZPL!.Replace("${descricaoProduto}", OsEvent.Product!.Name!);
            label.ZPL = label.ZPL!.Replace("${valorProduto}", OsEvent.Product!.Price.ToString());
            label.ZPL = label.ZPL!.Replace("${repostcode}", RepostEvent.AuthorizationCode!);
            label.ZPL = label.ZPL!.Replace("${diaAtual}", DateTime.Now.Day.ToString());
            label.ZPL = label.ZPL!.Replace("${mesAtual}", DateTime.Now.Month.ToString());
            label.ZPL = label.ZPL!.Replace("${anoAtual}", DateTime.Now.Year.ToString());

            Log.Information(label.ZPL);

            HttpResponseMessage response = await client.PostAsync($"http://{printer.Ip}/pstprnt", new StringContent(label.ZPL!, Encoding.UTF8));

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return StatusCode(200, "Impressão realizada com sucesso");
            }
            else
            {
                return StatusCode(500, "Erro ao realizar impressão");
            }
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());

            return StatusCode(500, "Internal Server Error >>> " + label.ZPL!);
        }
    }

}

