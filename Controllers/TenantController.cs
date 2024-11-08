using Microsoft.AspNetCore.Mvc;
using rmaesolutions.configInterface;
using Npgsql;
using Serilog;
using rmaesolutions.entities;

namespace rmaesolutions.Controllers;

[ApiController]
public class TenantController : ControllerBase
{
    /// <summary>
    /// Retorna todos os clientes.
    /// </summary>
    /// <returns>Uma lista de todas os clientes.</returns>
    /// <remarks>
    /// Exemplo de retorno:
    ///
    ///    [
    ///     {
    ///      "uuid": "string",
    ///      "name": "string",
    ///      "email": "string",
    ///      "cnpj": "string",
    ///      "udpatedat": "2023-04-19T18:27:56.123Z",
    ///      "createdat": "2023-04-19T18:27:56.123Z",
    ///     }
    ///    ]
    ///
    /// </remarks>
    /// <response code="200">Retorna todas as informações dos clientes.</response>
    /// <response code="204">Se a tabela de clientes estiver vazia.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>
 
    [HttpGet]
    [Route("v1/tenants/getall")]
    public async Task<dynamic> GetAllTenants()
    {
        try
        {   
            List<Tenant> tenants = [];

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using NpgsqlCommand cmd = new("SELECT * FROM tenants", connection);

            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

            while (reader.Read()){

                tenants.Add(new Tenant() { 
                    Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Cnpj = reader.GetString(reader.GetOrdinal("cnpj")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("isactive")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))

                });
            }

            return Ok(tenants);
            

        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpPost]
    [Route("v1/tenants/create")]
    public async Task<dynamic> CreateTenant([FromBody] TenantDTO tenant)
    {
        try
        {
            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using NpgsqlCommand cmd = new("SELECT * FROM tenants WHERE email = @email", connection);

            cmd.Parameters.AddWithValue("email", tenant.Email!);

            NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

            if (reader.HasRows)
            {   
                return StatusCode(409, "Cliente ja cadastrado.");
            }

            reader.Close();

            using NpgsqlCommand cmd2 = new("INSERT INTO tenants (uuid, name, email, cnpj, createdat, updatedat) VALUES (@uuid, @name, @email, @cnpj, @createdat, @updatedat)", connection);  

            cmd2.Parameters.AddWithValue("uuid", Guid.NewGuid());
            cmd2.Parameters.AddWithValue("name", tenant.Name!);
            cmd2.Parameters.AddWithValue("email", tenant.Email!);
            cmd2.Parameters.AddWithValue("cnpj", tenant.Cnpj!);
            cmd2.Parameters.AddWithValue("createdat", DateTime.Now.AddHours(-3));
            cmd2.Parameters.AddWithValue("updatedat", DateTime.Now.AddHours(-3));

            await cmd2.ExecuteNonQueryAsync();

            return Ok("Cliente criado com sucesso.");

        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpPut]
    [Route("v1/tenants/update")]
    public async Task<IActionResult> UpdateTenant([FromBody] TenantDTO tenant)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using NpgsqlCommand cmd = new("UPDATE tenants SET name = @name, email = @email, cnpj = @cnpj, updatedat = @updatedat, isactive = @isactive WHERE uuid = @uuid", connection);
            
            cmd.Parameters.AddWithValue("uuid", tenant.Uuid!);
            cmd.Parameters.AddWithValue("name", tenant.Name!);
            cmd.Parameters.AddWithValue("email",tenant.Email!);
            cmd.Parameters.AddWithValue("cnpj", tenant.Cnpj!);
            cmd.Parameters.AddWithValue("isactive", tenant.IsActive);
            cmd.Parameters.AddWithValue("updatedat", DateTime.Now.AddHours(-3)); 

            await cmd.ExecuteNonQueryAsync();

            return Ok("Cliente atualizado com sucesso");
            
        } catch (Exception e) {
            Log.Error(e.ToString());
            return StatusCode(500, "Internal Server Error");
        }
    }
}
