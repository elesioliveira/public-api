using Microsoft.AspNetCore.Mvc;
using rmaesolutions.configInterface;
using Npgsql;
using Serilog;
using rmaesolutions.entities;
using static BCrypt.Net.BCrypt;
using System.Text.RegularExpressions;
using rmaesolutions.dto;

namespace rmaesolutions.Controllers;

[ApiController]
public class UsersController : ControllerBase
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
    [Route("v1/users/getall")]
    public async Task<dynamic> GetAllUsers(string tenantuuid)
    {
        try
        {
            List<User> users = [];

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using NpgsqlCommand cmd = new("SELECT * FROM users WHERE tenantuuid = @tenantuuid", connection);

            cmd.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));

            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

            while (reader.Read())
            {

                users.Add(new User()
                {
                    Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                    UserName = reader.GetString(reader.GetOrdinal("username")),
                    FullName = reader.GetString(reader.GetOrdinal("fullname")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    TenantUuid = reader.GetGuid(reader.GetOrdinal("tenantuuid")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("isactive")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat")),
                });
            }

            return Ok(users);


        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return StatusCode(500, "Internal Server Error");
        }
    }

    /// <summary>
    /// Retorna um Usuario especificado.
    /// </summary>
    /// <param name="useruuid"></param>
    /// <returns></returns>
    /// <response code="200">Retorna o usuario especificado.</response>
    /// <response code="404">Usuario nao encontrado.</response>
    /// <response code="500">Erro interno do servidor.</response>

    [HttpGet]
    [Route("v1/users/get")]
    public IActionResult GetUser(string useruuid)
    {

        try
        {

            User user = new();

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using NpgsqlCommand cmd = new("SELECT * FROM users WHERE uuid = @uuid", connection);

            cmd.Parameters.AddWithValue("@uuid", Guid.Parse(useruuid));

            using NpgsqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {

                user = new User()
                {

                    Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                    UserName = reader.GetString(reader.GetOrdinal("username")),
                    FullName = reader.GetString(reader.GetOrdinal("fullname")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    TenantUuid = reader.GetGuid(reader.GetOrdinal("tenantuuid")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("isactive")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),

                };
            }

            return Ok(user);

        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return StatusCode(500, "Internal Server Error");
        }
    }

    /// <summary>
    /// Get all users for a specific tenant
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    /// <remarks>
    /// Sample request:
    /// GET /api/v1/users/create
    /// </remarks>
    /// <response code="200">Returns the list of users</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>

    [HttpPost]
    [Route("v1/users/create")]
    public async Task<dynamic> CreateTenant([FromBody] UserDTO user)
    {
        try
        {

            string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

            if (!Regex.IsMatch(user.Email!, emailPattern))
            {
                return BadRequest("Email invalido");
            }

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using NpgsqlCommand cmd = new("SELECT * FROM users WHERE email = @email", connection);

            cmd.Parameters.AddWithValue("email", user.Email!);

            NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

            if (reader.HasRows)
            {
                return StatusCode(409, "Usuario ja cadastrado");
            }

            reader.Close();

            using NpgsqlCommand cmd2 = new("INSERT INTO users (uuid, username, fullname, email, tenantuuid, isactive, passwordhash, createdat, updatedat) VALUES (@uuid, @username, @fullname, @email, @tenantuuid, @isactive, @password, @updatedat, @createdat);", connection);

            cmd2.Parameters.AddWithValue("uuid", Guid.NewGuid());
            cmd2.Parameters.AddWithValue("username", user.UserName!);
            cmd2.Parameters.AddWithValue("fullname", user.FullName!);
            cmd2.Parameters.AddWithValue("email", user.Email!);
            cmd2.Parameters.AddWithValue("isactive", true);
            cmd2.Parameters.AddWithValue("password", user.PasswordHash == null ? DBNull.Value : HashPassword(user.PasswordHash!));
            cmd2.Parameters.AddWithValue("tenantuuid", user.TenantUuid!);
            cmd2.Parameters.AddWithValue("createdat", DateTime.Now.AddHours(-3));
            cmd2.Parameters.AddWithValue("updatedat", DateTime.Now.AddHours(-3));

            await cmd2.ExecuteNonQueryAsync();

            return Ok("Usuario cadastrado com sucesso");

        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return StatusCode(500, "Internal Server Error");
        }
    }

    /// <summary>
    /// Atualiza informações do Usuario.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>

    [HttpPut]
    [Route("v1/users/update")]
    public async Task<IActionResult> UpdateUser([FromBody] UserDTO user)
    {
        try
        {
            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);
            connection.Open();

            string updateQuery = "UPDATE users SET fullname = @fullname, email = @email, username = @username, updatedat = @updatedat, isactive = @isactive";

            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                updateQuery += ", passwordhash = @password";
            }

            updateQuery += " WHERE uuid = @uuid;";

            using NpgsqlCommand cmd = new(updateQuery, connection);

            cmd.Parameters.AddWithValue("fullname", user.FullName!);
            cmd.Parameters.AddWithValue("email", user.Email!);
            cmd.Parameters.AddWithValue("username", user.UserName!);
            cmd.Parameters.AddWithValue("isactive", user.IsActive);
            cmd.Parameters.AddWithValue("uuid", user.Uuid!);
            cmd.Parameters.AddWithValue("updatedat", DateTime.Now.AddHours(-3));

            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                cmd.Parameters.AddWithValue("password", HashPassword(user.PasswordHash!));
            }

            await cmd.ExecuteNonQueryAsync();

            return Ok("Usuário atualizado com sucesso.");
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return StatusCode(500, "Internal Server Error");
        }
    }


    [HttpPost]
    [Route("v1/users/relationship/roles/create")]

    public IActionResult CreateUserRole([FromBody] List<UserRolesRelationshipDTO> userRole)
    {

        try
        {

            bool exists = false;

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using (NpgsqlCommand cmd = new("SELECT * FROM userroles WHERE useruuid = @useruuid", connection))
            {

                cmd.Parameters.AddWithValue("useruuid", userRole[0].UserUuid!);

                using NpgsqlDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    exists = true;
                }

                reader.Close();
            }

            if (exists)
            {
                using NpgsqlCommand cmd = new("DELETE FROM userroles WHERE useruuid = @useruuid", connection);

                cmd.Parameters.AddWithValue("useruuid", userRole[0].UserUuid!);

                cmd.ExecuteNonQuery();
            }

            foreach (var item in userRole)
            {
                using NpgsqlCommand cmd = new("INSERT INTO userroles (uuid, useruuid, roleuuid, tenantuuid, createdat, updatedat) VALUES (@uuid, @useruuid, @roleuuid, @tenantuuid, @createdat, @updatedat);", connection);

                cmd.Parameters.AddWithValue("uuid", Guid.NewGuid());
                cmd.Parameters.AddWithValue("useruuid", item.UserUuid!);
                cmd.Parameters.AddWithValue("roleuuid", item.RoleUuid!);
                cmd.Parameters.AddWithValue("tenantuuid", item.TenantUuid!);
                cmd.Parameters.AddWithValue("updatedat", DateTime.Now.AddHours(-3));
                cmd.Parameters.AddWithValue("createdat", DateTime.Now.AddHours(-3));

                cmd.ExecuteNonQuery();
            }

            return Ok("Permissões criadas com sucesso");

        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpGet]
    [Route("api/v1/user/relationship/role/get")]
    public IActionResult GetUserRoleRelationship(Guid userUuid)
    {
        List<UserRolesRelationshipDTO> relationships = [];

        List<Role> roles = [];

        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using (NpgsqlCommand command = new("SELECT * FROM userroles WHERE userUuid = @userUuid", connection))
            {

                command.Parameters.AddWithValue("userUuid", userUuid);

                using NpgsqlDataReader reader = command.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        relationships.Add(new UserRolesRelationshipDTO()
                        {
                            UserUuid = reader.GetGuid(reader.GetOrdinal("useruuid")),
                            RoleUuid = reader.GetGuid(reader.GetOrdinal("roleuuid")),
                            TenantUuid = reader.GetGuid(reader.GetOrdinal("tenantuuid")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
                        });
                    }

                }

                reader.Close();
            }

            if (relationships.Count > 0)
            {

                for (int i = 0; i < relationships.Count; i++)
                {
                    using NpgsqlCommand command = new("SELECT * FROM roles WHERE uuid = @uuid", connection);

                    command.Parameters.AddWithValue("uuid", relationships[i].RoleUuid!);

                    using NpgsqlDataReader reader = command.ExecuteReader();

                    reader.Read();

                    roles.Add(new Role
                    {
                        Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                        TenantUuid = reader.GetGuid(reader.GetOrdinal("tenantuuid")),
                        RoleName = reader.GetString(reader.GetOrdinal("rolename")),
                        Description = reader.GetString(reader.GetOrdinal("description")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
                    });
                }
            }

            return Ok(roles);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());
            return StatusCode(500, "Internal Server Error");
        }
    }
}
