using Microsoft.AspNetCore.Http;
using libcdm;
public class CDMInicializa
{
    private readonly RequestDelegate _next;


    private readonly CDMApiOperaciones _api;


    public CDMInicializa(RequestDelegate next, CDMApiOperaciones api)
    {
        _next = next;
        _api = api;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        
            CDMRequest cdm = new CDMRequest();
            cdm.idColeccion = context.Request.Cookies["cdm-idColeccion"] ?? "";
            cdm.data = new Dictionary<string, dynamic>();
            cdm.msg = "";
            cdm.status = 0;
            context.Items["cdm"] = cdm;
            
            // Llamar al siguiente middleware en la tubería
            await _next(context);
            return;
    }
}