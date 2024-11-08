using Microsoft.AspNetCore.Mvc;
using rmaesolutions.configInterface;
using Npgsql;
using Serilog;
using rmaesolutions.entities;

namespace rmaesolutions.Controllers;

[ApiController]
public class RolesController : ControllerBase
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
    [Route("v1/roles/getall")]
    public async Task<dynamic> GetAllRoles()
    {
        try
        {   
            List<Role> roles = [];

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using NpgsqlCommand cmd = new("SELECT * FROM roles", connection);

            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

            while (reader.Read()){

                roles.Add(new Role() { 
                    Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                    TenantUuid = reader.GetGuid(reader.GetOrdinal("tenantuuid")),
                    RoleName = reader.GetString(reader.GetOrdinal("rolename")),
                    Description = reader.GetString(reader.GetOrdinal("description")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))

                });
            }

            return Ok(roles);
            

        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpPost]
    [Route("v1/roles/create")]
    public async Task<dynamic> CreateRole([FromBody] RoleDTO role)
    {
        try
        {
            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using NpgsqlCommand cmd = new("SELECT * FROM roles WHERE rolename = @rolename AND tenantuuid = @tenantuuid", connection);

            cmd.Parameters.AddWithValue("@rolename", role.RoleName!);
            cmd.Parameters.AddWithValue("@tenantuuid", role.TenantUuid!);

            NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

            if (reader.HasRows)
            {   
                return StatusCode(409, "Role already exists.");
            }

            reader.Close();

            using NpgsqlCommand cmd2 = new("INSERT INTO roles (uuid, rolename, description, createdat, updatedat, tenantuuid) VALUES (@uuid, @rolename, @description, @createdat, @updatedat, @tenantuuid)", connection);

            cmd2.Parameters.AddWithValue("@uuid", Guid.NewGuid());
            cmd2.Parameters.AddWithValue("@rolename", role.RoleName!);
            cmd2.Parameters.AddWithValue("@description", role.Description!);
            cmd2.Parameters.AddWithValue("@tenantuuid", role.TenantUuid!);
            cmd2.Parameters.AddWithValue("createdat", DateTime.Now.AddHours(-3));
            cmd2.Parameters.AddWithValue("updatedat", DateTime.Now.AddHours(-3));

            await cmd2.ExecuteNonQueryAsync();

            return Ok("Role created successfully.");

        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpPut]
    [Route("v1/roles/update")]
    public async Task<IActionResult> UpdateRole([FromBody] RoleDTO role)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using NpgsqlCommand cmd = new("UPDATE roles SET rolename = @rolename, description = @description, updatedat = @updatedat WHERE uuid = @uuid", connection);

            cmd.Parameters.AddWithValue("@rolename", role.RoleName!);
            cmd.Parameters.AddWithValue("@description", role.Description!);
            cmd.Parameters.AddWithValue("@uuid", role.Uuid!);
            cmd.Parameters.AddWithValue("updatedat", DateTime.Now.AddHours(-3));

            await cmd.ExecuteNonQueryAsync();

            return Ok("Role updated successfully.");
            
        } catch (Exception e) {
            Log.Error(e.ToString());
            return StatusCode(500, "Internal Server Error");
        }
    }
}
