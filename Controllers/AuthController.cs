using Microsoft.AspNetCore.Mvc;
using rmaesolutions.configInterface;
using Npgsql;
using Serilog;
using rmaesolutions.entities;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using rmaesolutions.dto;
using static BCrypt.Net.BCrypt;

namespace rmaesolutions.Controllers;

[ApiController]
public class AuthController : ControllerBase
{      
    /// <summary>
    /// Faz o login do Usuário.
    /// </summary>
    /// <param name="email">Email do usuário</param>
    /// <param name="password">Senha do usuário</param>
    /// <returns>Retorna uma lista de funções do usuário</returns>
    /// <response code="200">Retorna as funções do usuário</response>
    /// <response code="403">Usuário não autorizado</response>
    /// <response code="500">Erro interno. Verifique os logs para mais detalhes.</response>
    
    [HttpGet]
    [Route("v1/auth/login")]
    public async Task<dynamic> Login(string email, string? password)
    {   

        try {

            Dictionary<string, dynamic> responseData = [];

            User user = new();

            string? hashedPasswordFromDB = null;

            List<Role> roles = [];
            List<Guid> roleUuids = [];

            using NpgsqlConnection conn = new(EnvInterface.SQLPostgres);

            conn.Open();

            using NpgsqlCommand cmd = new("SELECT * FROM users WHERE email = @email", conn);

            cmd.Parameters.AddWithValue("email", email);

            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

            if (reader.HasRows){

                while (reader.Read()) {

                    hashedPasswordFromDB = reader.IsDBNull(reader.GetOrdinal("passwordhash")) ? null : reader.GetString(reader.GetOrdinal("passwordhash"));
                    
                    user.IsActive = reader.GetBoolean(reader.GetOrdinal("isactive"));
                    user.Uuid = reader.GetGuid(reader.GetOrdinal("uuid"));
                    user.UserName = reader.GetString(reader.GetOrdinal("username"));
                    user.Email = email;
                    user.FullName = reader.GetString(reader.GetOrdinal("fullname"));
                    user.TenantUuid = reader.GetGuid(reader.GetOrdinal("tenantuuid"));

                    if (hashedPasswordFromDB == null){

                        return Unauthorized(user);
                    
                    } 
                }

                reader.Close();

            } else {

                return NotFound("Usuário não encontrado");

            }

            if (user.IsActive == false) return Unauthorized("Usuário inativo");

            user.PasswordHash = hashedPasswordFromDB;

            if (password != null) {

                if (Verify(password, user.PasswordHash)){

                    using NpgsqlCommand cmd2 = new("SELECT * FROM userroles WHERE useruuid = @useruuid", conn);

                    cmd2.Parameters.AddWithValue("useruuid", user.Uuid);

                    using NpgsqlDataReader reader2 = await cmd2.ExecuteReaderAsync();

                    if (reader.HasRows){

                        while (await reader2.ReadAsync()) {

                            roleUuids.Add(reader2.GetGuid(reader2.GetOrdinal("roleuuid")));
                            
                        }
                    }

                    reader2.Close();

                    if (roleUuids.Count > 0){

                        using NpgsqlCommand cmd3 = new("SELECT * FROM roles WHERE uuid = ANY(@roleUuids::uuid[])", conn);

                        cmd3.Parameters.AddWithValue("roleUuids", roleUuids);

                        using NpgsqlDataReader reader3 = await cmd3.ExecuteReaderAsync();

                        while (await reader3.ReadAsync()) {

                            roles.Add(new Role {
                                    Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                                    RoleName = reader.GetString(reader.GetOrdinal("rolename")),
                                    Description = reader.GetString(reader.GetOrdinal("description"))
                                    });
                        }

                        reader3.Close();

                    }

                    responseData.Add("user", user);
                    responseData.Add("roles", roles);

                    return Ok(responseData);

                }else{

                    return Unauthorized("Verifique sua senha");

                }
           }

           return Unauthorized("Coloque sua senha");
           
        }catch (Exception e){
        
            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }
    }

    // /// <summary>
    // /// Retorna uma Categoria.
    // /// </summary>
    // /// <param name="code">UUID da Categoria</param>
    // /// <returns>Retorna uma Categoria</returns>
    // /// <response code="200">Retorna uma Categoria</response>
    // /// <response code="500">Retorna uma mensagem de erro</response>
    
    // [HttpGet]
    // [Route("v1/ml/code")]
    // public async Task<dynamic> GetInitialToken(string code)
    // {   
    //     HttpClient httpclient = new ();

    //     try {

    //         // get token

    //         HttpResponseMessage response = await httpclient.PostAsync("https://api.mercadolibre.com/oauth/token", new StringContent($"grant_type=authorization_code&client_id={EnvInterface.MercadoLivreAppId}8&client_secret={EnvInterface.MercadoLivreSecretId}&code={code}&redirect_uri={EnvInterface.MercadoLivreRedirectUrl}", Encoding.UTF8, "application/x-www-form-urlencoded"));

    //         Dictionary<string, dynamic> token = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(await response.Content.ReadAsStringAsync())!;

    //         using NpgsqlConnection connection = new (EnvInterface.SQLPostgres);

    //         connection.Open();

    //         using NpgsqlCommand command2 = new ($"INSERT INTO authtoken (uuid, token, expiresat, createdat, system, refreshtoken, userid) VALUES (@uuid, @token, @expiresat, @createdat, @system, @refreshtoken, @userid);", connection);

    //         command2.Parameters.AddWithValue("@uuid", Guid.NewGuid());
    //         command2.Parameters.AddWithValue("@token", token["access_token"]);
    //         command2.Parameters.AddWithValue("@refreshtoken", token["refresh_token"]);
    //         command2.Parameters.AddWithValue("@userid", token["user_id"]);
    //         command2.Parameters.AddWithValue("@expiresat", DateTime.Now.AddHours(-3).AddSeconds(token["expires_in"]));
    //         command2.Parameters.AddWithValue("@createdat", DateTime.Now.AddHours(-3));
    //         command2.Parameters.AddWithValue("@system", "mercadolivre");

    //         command2.ExecuteNonQuery();     

    //         return Ok(token);      
            

    //     }catch (Exception e){
        
    //         Log.Error(e.ToString());

    //         return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
    //     }

    // }

    // /// <summary>
    // /// Retorna uma Categoria.
    // /// </summary>
    // /// <param name="userid">UUID da Categoria</param>
    // /// <returns>Retorna uma Categoria</returns>
    // /// <response code="200">Retorna uma Categoria</response>
    // /// <response code="500">Retorna uma mensagem de erro</response>
    
    // [HttpGet]
    // [Route("v1/ml/token")]
    // public async Task<dynamic> GetToken(string userid)
    // {   
    //     HttpClient httpclient = new ();

    //     try {

    //         using NpgsqlConnection connection = new (EnvInterface.SQLPostgres);

    //         connection.Open();

    //         using NpgsqlCommand command = new ($"SELECT * FROM authtoken WHERE userid = '{userid}' AND system = 'mercadolivre' ORDER BY createdat DESC LIMIT 1;", connection); 

    //         using NpgsqlDataReader reader = command.ExecuteReader();

    //         if (reader.Read())
    //         {
    //             if (reader.GetDateTime(2) > DateTime.Now.AddHours(-3))
    //             {
    //                 return Ok(reader.GetString(1));
    //             }
    //             else
    //             {   

    //                 HttpResponseMessage response = await httpclient.PostAsync("https://api.mercadolibre.com/oauth/token", new StringContent($"grant_type=refresh_token&client_id={EnvInterface.MercadoLivreAppId}&client_secret={EnvInterface.MercadoLivreSecretId}&refresh_token={reader.GetString(5)}", Encoding.UTF8, "application/x-www-form-urlencoded"));

    //                 reader.Close();

    //                 Dictionary<string, dynamic> token = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(await response.Content.ReadAsStringAsync())!;

    //                 using NpgsqlCommand command2 = new ($"INSERT INTO authtoken (uuid, token, expiresat, createdat, system, refreshtoken, userid) VALUES (@uuid, @token, @expiresat, @createdat, @system, @refreshtoken, @userid);", connection);

    //                 command2.Parameters.AddWithValue("@uuid", Guid.NewGuid());
    //                 command2.Parameters.AddWithValue("@token", token["access_token"]);
    //                 command2.Parameters.AddWithValue("@refreshtoken", token["refresh_token"]);
    //                 command2.Parameters.AddWithValue("@userid", token["user_id"]);
    //                 command2.Parameters.AddWithValue("@expiresat", DateTime.Now.AddHours(-3).AddSeconds(token["expires_in"]));
    //                 command2.Parameters.AddWithValue("@createdat", DateTime.Now.AddHours(-3));
    //                 command2.Parameters.AddWithValue("@system", "mercadolivre");

    //                 command2.ExecuteNonQuery();     

    //                 return Ok(token["access_token"]);
    //             }
    //         }
    //         else
    //         {
    //             return Problem("Token not found", null, (int)HttpStatusCode.NotFound);
    //         } 
            

    //     }catch (Exception e){
        
    //         Log.Error(e.ToString());

    //         return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
    //     }

    // }

    /// <summary>
    /// Retorna uma Categoria.
    /// </summary>
    /// <param name="code">UUID da Categoria</param>
    /// <returns>Retorna uma Categoria</returns>
    /// <response code="200">Retorna uma Categoria</response>
    /// <response code="500">Retorna uma mensagem de erro</response>
    
    // [HttpGet]
    // [Route("v1/bling/code")]
    // public async Task<dynamic> GetInitialBlingToken(string code)
    // {   
    //     HttpClient httpclient = new ();
    //     HttpClient httpclient2 = new ();

    //     try {

    //         string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(EnvInterface.BlingCredentials));

    //         httpclient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

    //         HttpResponseMessage response = await httpclient.PostAsync("https://www.bling.com.br/Api/v3/oauth/token", new StringContent($"grant_type=authorization_code&code={code}", Encoding.UTF8, "application/x-www-form-urlencoded"));

    //         Dictionary<string, dynamic> token = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(await response.Content.ReadAsStringAsync())!;

    //         httpclient2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token["access_token"]);

    //         HttpResponseMessage response2 = await httpclient2.GetAsync("https://www.bling.com.br/Api/v3/empresas/me/dados-basicos");

    //         Dictionary<string, dynamic> user = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(await response2.Content.ReadAsStringAsync())!;

    //         using NpgsqlConnection connection = new (EnvInterface.SQLPostgres);

    //         connection.Open();

    //         using NpgsqlCommand command2 = new ($"INSERT INTO authtoken (uuid, token, expiresat, createdat, system, refreshtoken, userid) VALUES (@uuid, @token, @expiresat, @createdat, @system, @refreshtoken, @userid);", connection);

    //         command2.Parameters.AddWithValue("@uuid", Guid.NewGuid());
    //         command2.Parameters.AddWithValue("@token", token["access_token"]);
    //         command2.Parameters.AddWithValue("@refreshtoken", token["refresh_token"]);
    //         command2.Parameters.AddWithValue("@userid", user["data"]["cnpj"].ToString().Replace(".", "").Replace("/", "").Replace("-", ""));
    //         command2.Parameters.AddWithValue("@expiresat", DateTime.Now.AddHours(-3).AddSeconds(token["expires_in"]));
    //         command2.Parameters.AddWithValue("@createdat", DateTime.Now.AddHours(-3));
    //         command2.Parameters.AddWithValue("@system", "bling");

    //         command2.ExecuteNonQuery();     

    //         return Ok(token);      
            

    //     }catch (Exception e){
        
    //         Log.Error(e.ToString());

    //         return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
    //     }

    // }

    // /// <summary>
    // /// Retorna uma Categoria.
    // /// </summary>
    // /// <param name="userid">UUID da Categoria</param>
    // /// <returns>Retorna uma Categoria</returns>
    // /// <response code="200">Retorna uma Categoria</response>
    // /// <response code="500">Retorna uma mensagem de erro</response>
    
    // [HttpGet]
    // [Route("v1/bling/token")]
    // public async Task<dynamic> GetBlingToken(string userid)
    // {   
    //     HttpClient httpclient = new ();

    //     try {

    //         using NpgsqlConnection connection = new (EnvInterface.SQLPostgres);

    //         connection.Open();

    //         using NpgsqlCommand command = new ($"SELECT * FROM authtoken WHERE userid = '{userid}' AND system = 'bling' ORDER BY createdat DESC LIMIT 1;", connection); 

    //         using NpgsqlDataReader reader = command.ExecuteReader();

    //         if (reader.Read())
    //         {
    //             if (reader.GetDateTime(2) > DateTime.Now.AddHours(-3))
    //             {
    //                 return Ok(reader.GetString(1));
    //             }
    //             else
    //             {   
    //                 string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(EnvInterface.BlingCredentials));

    //                 httpclient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

    //                 HttpResponseMessage response = await httpclient.PostAsync("https://www.bling.com.br/Api/v3/oauth/token", new StringContent($"grant_type=refresh_token&refresh_token={reader.GetString(5)}", Encoding.UTF8, "application/x-www-form-urlencoded"));

    //                 Dictionary<string, dynamic> token = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(await response.Content.ReadAsStringAsync())!;

    //                 using NpgsqlCommand command2 = new ($"INSERT INTO authtoken (uuid, token, expiresat, createdat, system, refreshtoken, userid) VALUES (@uuid, @token, @expiresat, @createdat, @system, @refreshtoken, @userid);", connection);

    //                 command2.Parameters.AddWithValue("@uuid", Guid.NewGuid());
    //                 command2.Parameters.AddWithValue("@token", token["access_token"]);
    //                 command2.Parameters.AddWithValue("@refreshtoken", token["refresh_token"]);
    //                 command2.Parameters.AddWithValue("@userid", userid);
    //                 command2.Parameters.AddWithValue("@expiresat", DateTime.Now.AddHours(-3).AddSeconds(token["expires_in"]));
    //                 command2.Parameters.AddWithValue("@createdat", DateTime.Now.AddHours(-3));
    //                 command2.Parameters.AddWithValue("@system", "bling");

    //                 reader.Close();

    //                 command2.ExecuteNonQuery();     

    //                 return Ok(token["access_token"]);
    //             }
    //         }
    //         else
    //         {
    //             return Problem("Token not found", null, (int)HttpStatusCode.NotFound);
    //         } 
            

    //     }catch (Exception e){
        
    //         Log.Error(e.ToString());

    //         return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
    //     }

    // }

    // /// <summary>
    // /// Retorna um token de validação de acesso para o sistema Kabum.
    // /// </summary>
    // /// <returns>Retorna um token de validação de acesso.</returns>
    // /// <response code="200">Retorna um token de acesso</response>
    // /// <response code="500">Erro interno. Verifique os logs para mais detalhes.</response>
    
    [HttpGet]
    [Route("v1/kabum/token")]
    public async Task<IActionResult> GetKabumToken()
    {   
        HttpClient httpclient = new ();

        try {

            using NpgsqlConnection connection = new (EnvInterface.SQLPostgres);

            connection.Open();

            using NpgsqlCommand command = new ($"SELECT * FROM authtoken WHERE userid = '{EnvInterface.KabumUser}' AND system = 'kabum' ORDER BY createdat DESC LIMIT 1;", connection); 

            using NpgsqlDataReader reader = command.ExecuteReader();

            if (reader.Read())
            {
                if (reader.GetDateTime(2) > DateTime.Now.AddHours(-3))
                {
                    return Ok(reader.GetString(1));
                }
                else
                {   

                    HttpResponseMessage response = await httpclient.PostAsync("https://technical-assistance.api.kabum.com.br/technical-assistance/v1/login", new StringContent($"email={EnvInterface.KabumUser}&password={EnvInterface.KabumPassword}", Encoding.UTF8, "application/x-www-form-urlencoded"));

                    KabumTokenDTO? token = JsonConvert.DeserializeObject<KabumTokenDTO>(await response.Content.ReadAsStringAsync());

                    using NpgsqlCommand command2 = new ($"INSERT INTO authtoken (uuid, token, expiresat, createdat, system, userid) VALUES (@uuid, @token, @expiresat, @createdat, @system, @userid);", connection);
                    command2.Parameters.AddWithValue("@uuid", Guid.NewGuid());
                    command2.Parameters.AddWithValue("@token", token!.Data!.Token!);
                    command2.Parameters.AddWithValue("@userid", EnvInterface.KabumUser);
                    command2.Parameters.AddWithValue("@expiresat", DateTime.Now.AddHours(-3).AddSeconds(token.Data!.ExpiresIn));
                    command2.Parameters.AddWithValue("@createdat", DateTime.Now.AddHours(-3));
                    command2.Parameters.AddWithValue("@system", "kabum");

                    reader.Close();

                    command2.ExecuteNonQuery();

                    return Ok(token.Data!.Token!);
                }
            }
            else
            {   

                HttpResponseMessage response = await httpclient.PostAsync("https://technical-assistance.api.kabum.com.br/technical-assistance/v1/login", new StringContent($"email={EnvInterface.KabumUser}&password={EnvInterface.KabumPassword}", Encoding.UTF8, "application/x-www-form-urlencoded"));

                KabumTokenDTO? token = JsonConvert.DeserializeObject<KabumTokenDTO>(await response.Content.ReadAsStringAsync());

                using NpgsqlCommand command2 = new ($"INSERT INTO authtoken (uuid, token, expiresat, createdat, system, userid) VALUES (@uuid, @token, @expiresat, @createdat, @system, @userid);", connection);

                command2.Parameters.AddWithValue("@uuid", Guid.NewGuid());
                command2.Parameters.AddWithValue("@token", token!.Data!.Token!);
                command2.Parameters.AddWithValue("@userid", EnvInterface.KabumUser);
                command2.Parameters.AddWithValue("@expiresat", DateTime.Now.AddHours(-3).AddSeconds(token.Data!.ExpiresIn));
                command2.Parameters.AddWithValue("@createdat", DateTime.Now.AddHours(-3));
                command2.Parameters.AddWithValue("@system", "kabum");

                reader.Close();

                command2.ExecuteNonQuery();     

                return Ok(token.Data!.Token!);
            } 
            

        }catch (Exception e){
        
            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }
}
