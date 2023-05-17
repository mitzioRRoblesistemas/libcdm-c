using Microsoft.AspNetCore.Http;
using libcdm;

public class CDMAnalizaSolicitud
{
    private readonly RequestDelegate _next;
    private readonly string _ruta;

    private readonly CDMApiOperaciones _api;


    public CDMAnalizaSolicitud(RequestDelegate next, string ruta, CDMApiOperaciones api)
    {
        _next = next;
        _ruta = ruta;
        _api = api;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path ==  _ruta)
        {
            try
            {
                CDMRequest Requestcdm = (CDMRequest)context.Items["cdm"]!;
                CDMApiResponse rta = await _api.api_getSolicitud(context.Request.Cookies["cdm-token"] ?? "", context.Request.Query["solicitud"].ToString());
                Requestcdm!.status = rta.status;
                Requestcdm!.msg = rta.msg;
                Requestcdm!.data!.Add("analizaSolicitud", rta);
                context.Items["cdm"] = Requestcdm;    
            }
            catch (System.Exception)
            {
                CDMRequest cdm = new CDMRequest();
                CDMRequest Requestcdm = (CDMRequest)context.Items["cdm"]!;
                cdm.status = 500;
                cdm.msg ="Error inesperado";
                Requestcdm!.status = cdm.status;
                Requestcdm!.msg = cdm.msg;
                Requestcdm!.data!.Add("analizaSolicitud", cdm);
                context.Items["cdm"] = Requestcdm;    
            }
        }
        await _next(context);
    }
}