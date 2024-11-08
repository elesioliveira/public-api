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
public class StorageController : ControllerBase
{
    /// <summary>
    /// Retorna todas as Storages.
    /// </summary>
    /// <returns>Uma lista de todas as Storages.</returns>
    /// <remarks>
    /// Exemplo de retorno:
    /// 
    ///     {
    ///        "uuid": "2a9c4a7f-7d54-4c8e-9a0b-1e44e5b9b467",
    ///        "name": "Depósito A",
    ///        "description": "Depósito A",
    ///        "location": "endereço/prateleira",
    ///        "createdAt": "2024-01-01T12:00:00Z",
    ///        "updatedAt": "2024-01-01T12:00:00Z"
    ///     },
    ///     {
    ///        "uuid": "6f9c5b2a-7f64-4a9e-9b2a-1e66d5c8d467",
    ///        "name": "Depósito B",
    ///        "description": "Depósito B",
    ///        "location": "endereço/prateleira",
    ///        "createdAt": "2024-02-01T12:00:00Z",
    ///        "updatedAt": "2024-02-01T12:00:00Z"
    ///     }
    /// </remarks>
    /// <response code="200">Retorna uma lista de Storages.</response>
    /// <response code="204">Se a tabela de Storages estiver vazia.</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/storage/getall")]
    public IActionResult GetAll(string tenantuuid)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();


            NpgsqlCommand command = new("SELECT * FROM storages WHERE tenantuuid = @tenantuuid", connection);
            command.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));
            NpgsqlDataReader reader = command.ExecuteReader();
            if (!reader.HasRows)
            {
                return NotFound("Nenhum estoque encontrado");
            }
            List<Storage> Storages = [];

            while (reader.Read())
            {
                Storages.Add(new Storage
                {
                    Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                    Location = reader.IsDBNull(reader.GetOrdinal("location")) ? null : reader.GetString(reader.GetOrdinal("location")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
                });
            }

            return Ok(Storages);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }

    /// <summary>
    /// Retorna uma Storage.
    /// </summary>
    /// <param name="uuid">UUID da Storage</param>
    /// <returns>Retorna uma Storage</returns>
    /// <remarks>
    /// Exemplo de retorno:
    /// 
    ///     {
    ///        "uuid": "2a9c4a7f-7d54-4c8e-9a0b-1e44e5b9b467",
    ///        "name": "Depósito A",
    ///        "description": "Depósito A",
    ///        "location": "endereço/prateleira",
    ///        "createdAt": "2024-01-01T12:00:00Z",
    ///        "updatedAt": "2024-01-01T12:00:00Z"
    ///     }
    /// </remarks>
    /// <response code="200">Retorna uma Storage</response>
    /// <response code="404">Storage não encontrada</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/storage/get")]
    public IActionResult Get(Guid uuid)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            NpgsqlCommand command = new("SELECT * FROM storages WHERE uuid = @uuid", connection);

            command.Parameters.AddWithValue("uuid", uuid);

            NpgsqlDataReader reader = command.ExecuteReader();

            reader.Read();

            Storage Storage = new()
            {
                Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                Location = reader.IsDBNull(reader.GetOrdinal("location")) ? null : reader.GetString(reader.GetOrdinal("location")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
            };

            return Ok(Storage);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }

    /// <summary>
    /// Retorna uma Storage.
    /// </summary>
    /// <param name="storageuuid">UUID da Storage</param>
    /// <returns>Retorna uma Storage</returns>
    /// <remarks>
    /// Exemplo de retorno:
    /// 
    ///     {
    ///        "uuid": "2a9c4a7f-7d54-4c8e-9a0b-1e44e5b9b467",
    ///        "name": "Depósito A",
    ///        "description": "Depósito A",
    ///        "location": "endereço/prateleira",
    ///        "createdAt": "2024-01-01T12:00:00Z",
    ///        "updatedAt": "2024-01-01T12:00:00Z"
    ///     }
    /// </remarks>
    /// <response code="200">Retorna uma Storage</response>
    /// <response code="404">Storage não encontrada</response>
    /// <response code="500">Erro interno do servidor. Verifique os logs para mais detalhes.</response>

    [HttpGet]
    [Route("v1/storage/getproducts")]
    public IActionResult GetProducts(Guid storageuuid)
    {
        try
        {   
        
            List<object> productCounts = [];

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            // SQL query to count occurrences of each productuuid
            NpgsqlCommand command = new("SELECT productuuid, COUNT(*) AS product_count FROM producttracking WHERE storageuuid = @storageuuid GROUP BY productuuid", connection);

            command.Parameters.AddWithValue("storageuuid", storageuuid);

            NpgsqlDataReader reader = command.ExecuteReader();

            // Loop through the results and add the productuuid and count to the list
            while (reader.Read())
            {
                Guid productUuid = reader.GetGuid(reader.GetOrdinal("productuuid"));
                int productCount = reader.GetInt32(reader.GetOrdinal("product_count"));

                productCounts.Add(new { ProductUuid = productUuid, Count = productCount });
            }

            return Ok(productCounts);
        

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }


    /// <summary>
    /// Cria ou atualiza uma Storage.
    /// </summary>
    /// <param name="obj">Objeto Storage</param>
    /// <param name="tenantuuid">O UUID do tenant para validar.</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    /// <remarks>
    /// Exemplo de corpo de requisição para criação:
    /// 
    ///     {
    ///        "name": "Depósito A",
    ///        "description": "Depósito A",
    ///        "location": "endereço/prateleira"
    ///     }
    /// 
    /// Exemplo de corpo de requisição para atualização:
    /// 
    ///     {
    ///        "uuid": "2a9c4a7f-7d54-4c8e-9a0b-1e44e5b9b467",
    ///        "name": "Nome Atualizado",
    ///        "description": "Descrição Atualizada",
    ///        "location": "Local Atualizado"
    ///     }
    /// </remarks>
    /// <response code="200">Retorna uma mensagem de sucesso</response>
    /// <response code="400">Storage já cadastrado</response>
    /// <response code="500">Retorna uma mensagem de erro</response>

    [HttpPost]
    [Route("v1/storage/create")]
    [Route("v1/storage/update")]
    public async Task<IActionResult> Upsert([FromBody] StorageDTO obj, string tenantuuid)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            await connection.OpenAsync();

            if (obj.Uuid == null)
            {

                using (NpgsqlCommand cmd = new("SELECT * FROM storages WHERE name = @name AND tenantuuid= @tenantuuid", connection))
                {

                    cmd.Parameters.AddWithValue("name", obj.Name!.ToLower());
                    cmd.Parameters.AddWithValue("tenantuuid", Guid.Parse(tenantuuid));

                    using NpgsqlDataReader reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                    {
                        return BadRequest("Storage já cadastrado");
                    }

                    reader.Close();
                }

                using (NpgsqlCommand cmd = new("INSERT INTO Storages (uuid,tenantuuid, name, description, location, createdat, updatedat) VALUES (@uuid,@tenantuuid, @name, @description, @location, @createdAt, @updatedAt)", connection))
                {

                    cmd.Parameters.AddWithValue("uuid", Guid.NewGuid());
                    cmd.Parameters.AddWithValue("tenantuuid", obj.TenantUuid!);
                    cmd.Parameters.AddWithValue("name", obj.Name);
                    cmd.Parameters.AddWithValue("description", obj.Description!);
                    cmd.Parameters.AddWithValue("location", obj.Location!);
                    cmd.Parameters.AddWithValue("createdAt", DateTime.Now.AddHours(-3));
                    cmd.Parameters.AddWithValue("updatedAt", DateTime.Now.AddHours(-3));

                    cmd.ExecuteNonQuery();
                }

                return Ok("Storage cadastrado com sucesso");

            }
            else
            {

                // REFACTOR USING STRINGBUILDER

                StringBuilder command = new("UPDATE storages SET");

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

                if (obj.Location != null)
                {
                    if (obj.Name != null || obj.Description != null)
                    {
                        command.Append(',');
                    }
                    command.Append("location = @location");
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

                    if (obj.Location != null)
                    {
                        cmd.Parameters.AddWithValue("location", obj.Location);
                    }

                    cmd.Parameters.AddWithValue("updatedAt", DateTime.Now.AddHours(-3));

                    cmd.ExecuteNonQuery();
                }

                return Ok("Storage atualizado com sucesso");
            }

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }
    }


}
