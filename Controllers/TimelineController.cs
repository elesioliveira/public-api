using Microsoft.AspNetCore.Mvc;
using rmaesolutions.configInterface;
using Npgsql;
using Serilog;
using rmaesolutions.entities;
using System.Net;
using rmaesolutions.dto;
using System.Text;

namespace rmaesolutions.Controllers;

[ApiController]
public class TimelineController : ControllerBase
{
    /// <summary>
    /// Retorna todas as Timelines.
    /// </summary>
    /// <returns>Retorna uma lista de Timelines</returns>
    /// <remarks>
    /// Exemplo de resposta de sucesso:
    ///
    ///     {
    ///         "timelines": [
    ///             {
    ///                 "uuid": "123e4567-e89b-12d3-a456-426614174001",
    ///                 "originUuid": "123e4567-e89b-12d3-a456-426614174002",
    ///                 "name": "Timeline Exemplo",
    ///                 "description": "Descrição da timeline",
    ///                 "createdAt": "2023-01-01T00:00:00Z",
    ///                 "updatedAt": "2023-01-02T00:00:00Z"
    ///             },
    ///             {
    ///                 "uuid": "123e4567-e89b-12d3-a456-426614174003",
    ///                 "originUuid": "123e4567-e89b-12d3-a456-426614174004",
    ///                 "name": "Outra Timeline",
    ///                 "description": "Outra descrição",
    ///                 "createdAt": "2023-01-03T00:00:00Z",
    ///                 "updatedAt": "2023-01-04T00:00:00Z"
    ///             }
    ///         ]
    ///     }
    ///
    /// </remarks>
    /// <response code="200">Retorna uma lista de Timelines</response>
    /// <response code="204">Se a tabela Timelines estiver vazia.</response>
    /// <response code="500">Retorna uma mensagem de erro</response>
    [HttpGet]
    [Route("v1/timeline/getall")]
    public async Task<IActionResult> GetAll(string tenantuuid)
    {
        try
        {
            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);
            await connection.OpenAsync(); // Use await para garantir que a conexão seja aberta antes de continuar

            NpgsqlCommand command = new("SELECT * FROM timelines WHERE tenantuuid = @tenantuuid", connection);
            command.Parameters.AddWithValue("@tenantuuid", Guid.Parse(tenantuuid));

            using NpgsqlDataReader reader = await command.ExecuteReaderAsync(); // Use ExecuteReaderAsync para leitura assíncrona

            if (!reader.HasRows)
            {
                return NotFound("Nenhum Timeline cadastrado!");
            }

            List<Timeline> timelines = new(); // Inicialize a lista corretamente

            while (await reader.ReadAsync()) // Use ReadAsync para leitura assíncrona
            {
                timelines.Add(new Timeline
                {
                    Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                    OriginUuid = reader.IsDBNull(reader.GetOrdinal("originuuid")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("originuuid")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
                });
            }

            return Ok(timelines);
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }
    }


    /// <summary>
    /// Retorna uma Timeline.
    /// </summary>
    /// <param name="uuid">UUID da Timeline</param>
    /// <returns>Retorna uma Timeline</returns>
    /// <remarks>
    /// Exemplo de resposta de sucesso:
    ///
    ///     {
    ///         "uuid": "123e4567-e89b-12d3-a456-426614174001",
    ///         "originUuid": "123e4567-e89b-12d3-a456-426614174002",
    ///         "name": "Timeline Exemplo",
    ///         "description": "Descrição da timeline",
    ///         "createdAt": "2023-01-01T00:00:00Z",
    ///         "updatedAt": "2023-01-02T00:00:00Z"
    ///     }
    ///
    /// </remarks>
    /// <response code="200">Retorna uma Timeline</response>
    /// <response code="500">Retorna uma mensagem de erro</response>

    [HttpGet]
    [Route("v1/timeline/get")]
    public IActionResult Get(Guid uuid)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            NpgsqlCommand command = new("SELECT * FROM timelines WHERE uuid = @uuid", connection);

            command.Parameters.AddWithValue("uuid", uuid);

            NpgsqlDataReader reader = command.ExecuteReader();

            reader.Read();

            Timeline timeline = new()
            {
                Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                OriginUuid = reader.IsDBNull(reader.GetOrdinal("originuuid")) ? null : reader.GetGuid(reader.GetOrdinal("originuuid")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))

            };

            return Ok(timeline);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }

    }

    /// <summary>
    /// Cria ou atualiza uma Timeline.
    /// </summary>
    /// <param name="obj">Objeto Timeline</param>
    /// <param name="tenantuuid">O UUID do tenant para validar.</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    /// <remarks>
    /// Exemplo de request para criar uma nova Timeline:
    ///
    ///     POST /v1/timeline/create
    ///     {
    ///         "originUuid": "123e4567-e89b-12d3-a456-426614174002",
    ///         "name": "Nova Timeline",
    ///         "description": "Descrição da nova timeline"
    ///     }
    ///
    /// Exemplo de request para atualizar uma Timeline existente:
    ///
    ///     POST /v1/timeline/update
    ///     {
    ///         "uuid": "123e4567-e89b-12d3-a456-426614174001",
    ///         "originUuid": "123e4567-e89b-12d3-a456-426614174002",
    ///         "name": "Timeline Atualizada",
    ///         "description": "Descrição atualizada da timeline"
    ///     }
    ///
    /// </remarks>
    /// <response code="200">Retorna uma mensagem de sucesso</response>
    /// <response code="500">Retorna uma mensagem de erro</response>
    /// 
    [HttpPost]
    [Route("v1/timeline/create")]
    [Route("v1/timeline/update")]
    public IActionResult Upsert([FromBody] TimelineDTO obj, string tenantuuid)
    {
        try
        {
            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);
            connection.Open();

            if (obj.Uuid == null)
            {
                using (NpgsqlCommand cmd = new("SELECT * FROM timelines WHERE name = @name AND tenantuuid = @tenantuuid", connection))
                {
                    cmd.Parameters.AddWithValue("name", obj.Name!.ToLower());
                    cmd.Parameters.AddWithValue("tenantuuid", Guid.Parse(tenantuuid));

                    using NpgsqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        return BadRequest("Timeline já cadastrado");
                    }
                }

                using (NpgsqlCommand cmd = new("INSERT INTO timelines (uuid, tenantuuid, originuuid, name, description, createdat, updatedat) VALUES (@uuid, @tenantuuid, @originuuid, @name, @description, @createdAt, @updatedAt)", connection))
                {
                    cmd.Parameters.AddWithValue("uuid", Guid.NewGuid());
                    cmd.Parameters.AddWithValue("tenantuuid", obj.TenantUuid!);
                    cmd.Parameters.AddWithValue("originuuid", obj.OriginUuid == null ? DBNull.Value : obj.OriginUuid);
                    cmd.Parameters.AddWithValue("name", obj.Name);
                    cmd.Parameters.AddWithValue("description", obj.Description!);
                    cmd.Parameters.AddWithValue("createdAt", DateTime.Now.AddHours(-3));
                    cmd.Parameters.AddWithValue("updatedAt", DateTime.Now.AddHours(-3));

                    cmd.ExecuteNonQuery();
                }

                return Ok("Timeline cadastrado com sucesso");
            }
            else
            {
                StringBuilder command = new("UPDATE timelines SET");

                bool appendComma = false;

                void AppendCommaIfRequired()
                {
                    if (appendComma) command.Append(", ");
                    appendComma = true;
                }

                if (obj.OriginUuid != null) { AppendCommaIfRequired(); command.Append(" originuuid = @originUuid"); }
                if (obj.Name != null) { AppendCommaIfRequired(); command.Append(" name = @name"); }
                if (obj.Description != null) { AppendCommaIfRequired(); command.Append(" description = @description"); }

                command.Append(", updatedat = @updatedAt WHERE uuid = @uuid");

                using (NpgsqlCommand cmd = new(command.ToString(), connection))
                {
                    cmd.Parameters.AddWithValue("uuid", obj.Uuid);

                    if (obj.OriginUuid != null) cmd.Parameters.AddWithValue("originUuid", obj.OriginUuid);
                    if (obj.Name != null) cmd.Parameters.AddWithValue("name", obj.Name);
                    if (obj.Description != null) cmd.Parameters.AddWithValue("description", obj.Description);

                    cmd.Parameters.AddWithValue("updatedAt", DateTime.Now.AddHours(-3));

                    cmd.ExecuteNonQuery();
                }

                return Ok("Timeline atualizado com sucesso");
            }
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }
    }


    /// <summary>
    /// Cria ou atualiza uma Etapa da Timeline.
    /// </summary>
    /// <param name="obj">Objeto TimelineStep</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    /// <remarks>
    /// Exemplo de request para criar uma nova Etapa da Timeline:
    ///
    ///     POST /v1/timelinesteps/create
    ///     {
    ///         "timelineUuid": "123e4567-e89b-12d3-a456-426614174000",
    ///         "originUuid": "123e4567-e89b-12d3-a456-426614174002",
    ///         "name": "Nova Etapa",
    ///         "instructions": "Instruções da nova etapa"
    ///     }
    ///
    /// Exemplo de request para atualizar uma Etapa da Timeline existente:
    ///
    ///     POST /v1/timelinesteps/update
    ///     {
    ///         "uuid": "123e4567-e89b-12d3-a456-426614174001",
    ///         "timelineUuid": "123e4567-e89b-12d3-a456-426614174000",
    ///         "originUuid": "123e4567-e89b-12d3-a456-426614174002",
    ///         "name": "Etapa Atualizada",
    ///         "instructions": "Instruções atualizadas da etapa"
    ///     }
    ///
    /// </remarks>
    /// <response code="200">Retorna uma mensagem de sucesso</response>
    /// <response code="500">Retorna uma mensagem de erro</response>

    [HttpPost]
    [Route("v1/timelinesteps/create")]
    [Route("v1/timelinesteps/update")]
    public IActionResult RelationshipStepsCreate([FromBody] TimelineStepDTO obj)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            if (obj.Uuid == null)
            {

                using NpgsqlCommand cmd = new("INSERT INTO timelinesteps (uuid,tenantuuid, timelineuuid, originuuid, name, instructions, createdat, updatedat) VALUES (@uuid,@tenantuuid, @timelineuuid, @originuuid, @name, @instructions, @createdat, @updatedat)", connection);

                cmd.Parameters.AddWithValue("uuid", Guid.NewGuid());
                cmd.Parameters.AddWithValue("tenantuuid", obj.TenantUuid!);
                cmd.Parameters.AddWithValue("timelineuuid", obj.TimelineUuid!);
                cmd.Parameters.AddWithValue("originuuid", obj.OriginUuid == null ? DBNull.Value : obj.OriginUuid);
                cmd.Parameters.AddWithValue("name", obj.Name!);
                cmd.Parameters.AddWithValue("instructions", obj.Instructions!);
                cmd.Parameters.AddWithValue("createdat", DateTime.Now.AddHours(-3));
                cmd.Parameters.AddWithValue("updatedat", DateTime.Now.AddHours(-3));

                cmd.ExecuteNonQuery();

            }
            else
            {

                StringBuilder command = new("UPDATE timelinesteps SET");

                bool appendComma = false;

                void AppendCommaIfRequired()
                {
                    if (appendComma) command.Append(", ");
                    appendComma = true;
                }

                if (obj.TimelineUuid != null) { AppendCommaIfRequired(); command.Append(" timelineuuid = @timelineUuid"); }
                if (obj.OriginUuid != null) { AppendCommaIfRequired(); command.Append(" originuuid = @originUuid"); }
                if (obj.Name != null) { AppendCommaIfRequired(); command.Append(" name = @name"); }
                if (obj.Instructions != null) { AppendCommaIfRequired(); command.Append(" instructions = @instructions"); }

                command.Append(", updatedat = @updatedAt WHERE uuid = @uuid");

                using NpgsqlCommand cmd = new(command.ToString(), connection);

                cmd.Parameters.AddWithValue("uuid", obj.Uuid);

                if (obj.TimelineUuid != null) cmd.Parameters.AddWithValue("timelineUuid", obj.TimelineUuid);
                if (obj.OriginUuid != null) cmd.Parameters.AddWithValue("originUuid", obj.OriginUuid);
                if (obj.Name != null) cmd.Parameters.AddWithValue("name", obj.Name);
                if (obj.Instructions != null) cmd.Parameters.AddWithValue("instructions", obj.Instructions);

                cmd.Parameters.AddWithValue("updatedAt", DateTime.Now.AddHours(-3));

                cmd.ExecuteNonQuery();

            }

            return Ok("Etapa criado com sucesso");

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>
    /// Retorna todas as Etapas de uma Timeline.
    /// </summary>
    /// <param name="timelineuuid">UUID da Timeline</param>
    /// <returns>Retorna uma lista de Etapas da Timeline</returns>
    /// <remarks>
    /// Exemplo de request:
    ///
    ///     GET /v1/timelinesteps/get?timelineuuid=123e4567-e89b-12d3-a456-426614174000
    ///
    /// </remarks>
    /// <response code="200">Retorna uma lista de Etapas da Timeline</response>
    /// <response code="500">Retorna uma mensagem de erro</response>

    [HttpGet]
    [Route("v1/timelinesteps/get")]
    public IActionResult TimelineStepsGet(Guid timelineuuid)
    {
        try
        {

            List<TimelineStep> steps = [];

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using (NpgsqlCommand command = new("SELECT * FROM timelinesteps WHERE timelineuuid = @timelineUuid", connection))
            {

                command.Parameters.AddWithValue("timelineUuid", timelineuuid);

                using NpgsqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    steps.Add(new TimelineStep
                    {
                        Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                        TimelineUuid = reader.GetGuid(reader.GetOrdinal("timelineuuid")),
                        OriginUuid = reader.IsDBNull(reader.GetOrdinal("originuuid")) ? null : reader.GetGuid(reader.GetOrdinal("originuuid")),
                        Name = reader.GetString(reader.GetOrdinal("name")),
                        Instructions = reader.GetString(reader.GetOrdinal("instructions")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))

                    });
                }

            }


            return Ok(steps);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>
    /// Cria ou atualiza um relacionamento de etapas de Timeline.
    /// </summary>
    /// <param name="obj">Objeto TimelineStepsRelationshipDTO</param>
    /// <returns>Retorna uma mensagem de sucesso ou erro</returns>
    /// <remarks>
    /// Exemplo de request:
    ///
    ///     POST /v1/timeline/relationship/steps/create
    ///     {
    ///         "fromTimelineStepUuid": "123e4567-e89b-12d3-a456-426614174000",
    ///         "toTimelineStepUuid": "123e4567-e89b-12d3-a456-426614174001",
    ///         "fromStorageUuid": "123e4567-e89b-12d3-a456-426614174002",
    ///         "toStorageUuid": "123e4567-e89b-12d3-a456-426614174003",
    ///         "fromStatusUuid": "123e4567-e89b-12d3-a456-426614174004",
    ///         "toStatusUuid": "123e4567-e89b-12d3-a456-426614174005"
    ///     }
    ///
    ///     POST /v1/timeline/relationship/steps/update
    ///     {
    ///         "uuid": "123e4567-e89b-12d3-a456-426614174000",
    ///         "fromTimelineStepUuid": "123e4567-e89b-12d3-a456-426614174000",
    ///         "toTimelineStepUuid": "123e4567-e89b-12d3-a456-426614174001",
    ///         "fromStorageUuid": "123e4567-e89b-12d3-a456-426614174002",
    ///         "toStorageUuid": "123e4567-e89b-12d3-a456-426614174003",
    ///         "fromStatusUuid": "123e4567-e89b-12d3-a456-426614174004",
    ///         "toStatusUuid": "123e4567-e89b-12d3-a456-426614174005"
    ///     }
    ///
    /// </remarks>
    /// <response code="200">Retorna uma mensagem de sucesso</response>
    /// <response code="500">Retorna uma mensagem de erro</response>

    [HttpPost]
    [Route("v1/timeline/relationship/steps/create")]
    [Route("v1/timeline/relationship/steps/update")]
    public IActionResult RelationshipStepsCreate([FromBody] TimelineStepsRelationshipDTO obj)
    {
        try
        {

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            if (obj.Uuid == null)
            {

                using NpgsqlCommand cmd = new("INSERT INTO timelinestepsrelationship (uuid, tenantuuid, fromtimelinestepuuid, totimelinestepuuid, fromstorageuuid, tostorageuuid, fromstatusuuid, tostatusuuid, createdat, updatedat) VALUES (@uuid, @tenantuuid, @fromTimelineStepUuid, @toTimelineStepUuid, @fromStorageUuid, @toStorageUuid, @fromStatusUuid, @toStatusUuid, @createdAt, @updatedAt)", connection);

                cmd.Parameters.AddWithValue("uuid", Guid.NewGuid());
                cmd.Parameters.AddWithValue("tenantuuid", obj.TenantUuid!);
                cmd.Parameters.AddWithValue("fromTimelineStepUuid", obj.FromTimelineStepUuid!);
                cmd.Parameters.AddWithValue("toTimelineStepUuid", obj.ToTimelineStepUuid!);
                cmd.Parameters.AddWithValue("fromStorageUuid", obj.FromStorageUuid == null ? DBNull.Value : obj.FromStorageUuid);
                cmd.Parameters.AddWithValue("toStorageUuid", obj.ToStorageUuid == null ? DBNull.Value : obj.ToStorageUuid);
                cmd.Parameters.AddWithValue("fromStatusUuid", obj.FromStatusUuid == null ? DBNull.Value : obj.FromStatusUuid);
                cmd.Parameters.AddWithValue("toStatusUuid", obj.ToStatusUuid == null ? DBNull.Value : obj.ToStatusUuid);
                cmd.Parameters.AddWithValue("createdAt", DateTime.Now.AddHours(-3));
                cmd.Parameters.AddWithValue("updatedAt", DateTime.Now.AddHours(-3));

                cmd.ExecuteNonQuery();

            }
            else
            {

                StringBuilder command = new("UPDATE timelinesteps SET");

                bool appendComma = false;

                void AppendCommaIfRequired()
                {
                    if (appendComma) command.Append(", ");
                    appendComma = true;
                }

                if (obj.FromTimelineStepUuid != null) { AppendCommaIfRequired(); command.Append(" fromtimelinestepuuid = @fromTimelineStepUuid"); }
                if (obj.ToTimelineStepUuid != null) { AppendCommaIfRequired(); command.Append(" totimelinestepuuid = @toTimelineStepUuid"); }
                if (obj.FromStorageUuid != null) { AppendCommaIfRequired(); command.Append(" fromstorageuuid = @fromStorageUuid"); }
                if (obj.ToStorageUuid != null) { AppendCommaIfRequired(); command.Append(" tostorageuuid = @toStorageUuid"); }
                if (obj.FromStatusUuid != null) { AppendCommaIfRequired(); command.Append(" fromstatusuuid = @fromStatusUuid"); }
                if (obj.ToStatusUuid != null) { AppendCommaIfRequired(); command.Append(" tostatusuuid = @toStatusUuid"); }

                command.Append(", updatedat = @updatedAt WHERE uuid = @uuid");

                using NpgsqlCommand cmd = new(command.ToString(), connection);

                cmd.Parameters.AddWithValue("uuid", obj.Uuid);

                if (obj.FromTimelineStepUuid != null) cmd.Parameters.AddWithValue("fromTimelineStepUuid", obj.FromTimelineStepUuid);
                if (obj.ToTimelineStepUuid != null) cmd.Parameters.AddWithValue("toTimelineStepUuid", obj.ToTimelineStepUuid);
                if (obj.FromStorageUuid != null) cmd.Parameters.AddWithValue("fromStorageUuid", obj.FromStorageUuid);
                if (obj.ToStorageUuid != null) cmd.Parameters.AddWithValue("toStorageUuid", obj.ToStorageUuid);
                if (obj.FromStatusUuid != null) cmd.Parameters.AddWithValue("fromStatusUuid", obj.FromStatusUuid);
                if (obj.ToStatusUuid != null) cmd.Parameters.AddWithValue("toStatusUuid", obj.ToStatusUuid);

                cmd.Parameters.AddWithValue("updatedAt", DateTime.Now.AddHours(-3));

                cmd.ExecuteNonQuery();

            }

            return Ok("Relacionamento criado com sucesso");

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>
    /// Retorna o relacionamento de etapas de uma Timeline.
    /// </summary>
    /// <param name="timelinestepuuid">UUID da etapa da Timeline</param>
    /// <returns>Retorna o relacionamento de etapas de uma Timeline</returns>
    /// <remarks>
    /// Exemplo de request:
    ///
    ///     GET /v1/timeline/relationship/steps/get?timelinestepuuid=123e4567-e89b-12d3-a456-426614174000
    ///
    /// </remarks>
    /// <response code="200">Retorna o relacionamento de etapas de uma Timeline</response>
    /// <response code="500">Retorna uma mensagem de erro</response>

    [HttpGet]
    [Route("v1/timeline/relationship/steps/get")]
    public IActionResult RelationshipStepsGet(Guid timelinestepuuid)
    {
        try
        {

            List<TimelineStepsRelationshipDTO> stepsrelation = [];

            using NpgsqlConnection connection = new(EnvInterface.SQLPostgres);

            connection.Open();

            using (NpgsqlCommand command = new("SELECT * FROM timelinestepsrelationship WHERE fromtimelinestepuuid = @timelinestepuuid", connection))
            {

                command.Parameters.AddWithValue("timelinestepuuid", timelinestepuuid);

                using NpgsqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    stepsrelation.Add(new TimelineStepsRelationshipDTO
                    {
                        Uuid = reader.GetGuid(reader.GetOrdinal("uuid")),
                        FromTimelineStepUuid = reader.GetGuid(reader.GetOrdinal("fromtimelinestepuuid")),
                        ToTimelineStepUuid = reader.GetGuid(reader.GetOrdinal("totimelinestepuuid")),
                        FromStorageUuid = reader.IsDBNull(reader.GetOrdinal("fromstorageuuid")) ? null : reader.GetGuid(reader.GetOrdinal("fromstorageuuid")),
                        ToStorageUuid = reader.IsDBNull(reader.GetOrdinal("tostorageuuid")) ? null : reader.GetGuid(reader.GetOrdinal("tostorageuuid")),
                        FromStatusUuid = reader.IsDBNull(reader.GetOrdinal("fromstatusuuid")) ? null : reader.GetGuid(reader.GetOrdinal("fromstatusuuid")),
                        ToStatusUuid = reader.IsDBNull(reader.GetOrdinal("tostatusuuid")) ? null : reader.GetGuid(reader.GetOrdinal("tostatusuuid")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("createdat")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updatedat"))
                    });
                }

            }


            return Ok(stepsrelation);

        }
        catch (Exception e)
        {

            Log.Error(e.ToString());

            return Problem(e.ToString(), null, (int)HttpStatusCode.InternalServerError);
        }
    }
}
