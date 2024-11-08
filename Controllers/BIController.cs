using Microsoft.AspNetCore.Mvc;
using Npgsql;
using rmaesolutions.configInterface;
using rmaesolutions.dto;

namespace rmaesolutions.Controllers;

[ApiController]
public class BIController : ControllerBase
{      
    /// <summary>
    /// Retorna uma Categoria.
    /// </summary>
    /// <returns>Retorna uma Categoria</returns>
    /// <response code="200">Retorna uma Categoria</response>
    /// <response code="500">Retorna uma mensagem de erro</response>
    
    [HttpGet]
    [Route("v1/bi/history/pipeline")]
    public async Task<IActionResult> GetHistoryPipelinesProducts(DateTime? startDate, DateTime? endDate)
    {

        await Task.Delay(0);

        List<PipelineCountsDTO> pipelineCounts = [];

        using NpgsqlConnection connection = new (EnvInterface.SQLPostgres);

        connection.Open();

        using NpgsqlCommand command = new (@"SELECT 
                                                s.name AS status_name,
                                                DATE(ph.createdat) AS date,
                                                COUNT(*) AS product_count
                                            FROM 
                                                productshistory ph
                                            JOIN 
                                                status s ON ph.tostatusuuid = s.uuid
                                            WHERE
                                                ph.createdat BETWEEN @startDate AND @endDate
                                            GROUP BY 
                                                s.name, 
                                                DATE(ph.createdat)
                                            ORDER BY 
                                                date, 
                                                s.name;", connection);
        
        command.Parameters.AddWithValue("startDate", startDate == null ? DateTime.Now.AddDays(-30) : startDate);
        command.Parameters.AddWithValue("endDate", endDate == null ? DateTime.Now : endDate);
        
        using NpgsqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            pipelineCounts.Add(new PipelineCountsDTO
            {
                StatusName = reader.GetString(reader.GetOrdinal("status_name")),
                Date = reader.GetDateTime(reader.GetOrdinal("date")),
                ProductCount = reader.GetInt32(reader.GetOrdinal("product_count"))
            });
        }
    
        return Ok(pipelineCounts);

    }

    /// <summary>
    /// Retorna uma Categoria.
    /// </summary>
    /// <returns>Retorna uma Categoria</returns>
    /// <response code="200">Retorna uma Categoria</response>
    /// <response code="500">Retorna uma mensagem de erro</response>
    
    [HttpGet]
    [Route("v1/bi/current/pipeline")]
    public async Task<IActionResult> GetCurrentPipelinesProducts()
    {

        await Task.Delay(0);

        List<PipelineCountsDTO> pipelineCounts = [];

        using NpgsqlConnection connection = new (EnvInterface.SQLPostgres);

        connection.Open();

        using NpgsqlCommand command = new (@"SELECT timelinesteps.name as status_name, COUNT(*) as product_count
                                                FROM public.producttracking
                                                JOIN public.timelinesteps ON public.producttracking.timelinestepuuid = public.timelinesteps.uuid
                                                GROUP BY timelinesteps.name;", connection);
        
        using NpgsqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            pipelineCounts.Add(new PipelineCountsDTO
            {
                StatusName = reader.GetString(reader.GetOrdinal("status_name")),
                ProductCount = reader.GetInt32(reader.GetOrdinal("product_count"))
            });
        }
    
        return Ok(pipelineCounts);

    }
    
}
