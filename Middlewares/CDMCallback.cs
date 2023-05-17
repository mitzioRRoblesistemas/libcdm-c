using Microsoft.AspNetCore.Http;
using libcdm;
public class CDMCallback
{
    private readonly RequestDelegate _next;
    private readonly string _ruta;

    private readonly CDMApiOperaciones _api;


    public CDMCallback(RequestDelegate next, string rutaCallback, CDMApiOperaciones api)
    {
        _next = next;
        _ruta = rutaCallback;
        _api = api;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        
        if (context.Request.Path == _ruta)
        {
            try
            {
                CDMRequest cdm = new CDMRequest();
                CDMRequest Requestcdm = (CDMRequest)context.Items["cdm"]!;
                if (context.Request.Query.ContainsKey("tipo") && context.Request.Query["tipo"].ToString() == "login")
                {
                    context.Response.Cookies.Delete("cdm-token");
                    context.Response.Cookies.Append("cdm-idSesion", context.Request.Query["idSesion"].ToString() ?? "", new CookieOptions
                    {MaxAge = TimeSpan.FromDays(365),HttpOnly = false});
                    context.Response.Cookies.Append("cdm-keyLogin", context.Request.Query["keyLogin"].ToString() ?? "", new CookieOptions
                    {MaxAge = TimeSpan.FromDays(365),HttpOnly = false});
                    CDMApiResponse rta = await _api.api_getToken(context.Request.Query["codigoAutorizacion"].ToString());
                    if (rta.status == 200)
                    {                      
                        context.Response.Cookies.Append("cdm-token", rta.data?["token"], new CookieOptions
                        {MaxAge = TimeSpan.FromDays(365),HttpOnly = false});
                        CDMApiResponse perfil = await _api.api_getPerfil(rta.data?["token"]);
                        if (perfil.status == 200)
                        {
                            Requestcdm.idColeccion = perfil.data?["id"];
                            context.Response.Cookies.Append("cdm-idColeccion", perfil.data?["id"], new CookieOptions
                            {MaxAge = TimeSpan.FromDays(365),HttpOnly = false});
                            Requestcdm!.data!.Add("getPerfil", perfil);
                        }
                        Requestcdm.status = perfil.status;
                        Requestcdm.msg = perfil.msg;
                        Requestcdm.token = rta.data?["token"];
                        Requestcdm!.data!.Add("origenUri", context.Request.Query["origenUri"].ToString());
                        context.Items["cdm"] = Requestcdm;    
                        await _next(context);
                        return;
                    }
                }
                if (context.Request.Query.ContainsKey("tipo") && context.Request.Query["tipo"].ToString() == "solicitud")
                {
                        if(context.Request.Query["metodo"].ToString() == "facetec"){
                            context.Response.Cookies.Append("cdm-solicitud-facetec", context.Request.Query["solicitud"].ToString() ?? "", new CookieOptions
                            {MaxAge = TimeSpan.FromDays(365),HttpOnly = false});
                        }
                        if(context.Request.Query["metodo"].ToString() == "otp"){
                            context.Response.Cookies.Append("cdm-solicitud-otp", context.Request.Query["solicitud"].ToString() ?? "", new CookieOptions
                            {MaxAge = TimeSpan.FromDays(365),HttpOnly = false});
                        }
                        context.Response.Redirect(context.Request.Query["origenUri"].ToString() + "?solicitud=" + context.Request.Query["solicitud"].ToString());
                        return;
                }
                cdm.status = 400;
                cdm.msg ="tipo de callback no reconocido";
                Requestcdm.msg=cdm.msg;
                Requestcdm.status=cdm.status;
                Requestcdm!.data!.Add("getPerfil", cdm);
                Requestcdm!.data!.Add("origenUri", context.Request.Query["origenUri"].ToString() ?? "");
                context.Items["cdm"] = Requestcdm;    
            }
            catch (System.Exception)
            {
                CDMRequest cdm = new CDMRequest();
                CDMRequest Requestcdm = (CDMRequest)context.Items["cdm"]!;
                cdm.status = 500;
                cdm.msg ="Error inesperado";
                Requestcdm.msg=cdm.msg;
                Requestcdm.status=cdm.status;
                Requestcdm!.data!.Add("getPerfil", cdm);
                Requestcdm!.data!.Add("origenUri", context.Request.Query["origenUri"].ToString() ?? "");
                context.Items["cdm"] = Requestcdm;    
            }
        }
        await _next(context);
        return;
    }
}