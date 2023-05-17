using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace libcdm
{ 
    public class CDMApiOperaciones
{
    private readonly List<string> errores = new List<string>();
    private readonly string? clientId;
    private readonly string? clientSecret;
    private readonly string? redirectUri;
    private readonly string? urlServerApi;
    private readonly string? urlServerAuth;
    private readonly Dictionary<string, dynamic>? apiKey;

    public CDMApiOperaciones(CDMApiOperacionesOpciones opciones)
    {
        clientId = opciones.ClientId ?? throw new ArgumentNullException(nameof(opciones.ClientId), "Falta clientId");
        clientSecret = opciones.ClientSecret ?? throw new ArgumentNullException(nameof(opciones.ClientSecret), "Falta clientSecret");
        redirectUri = opciones.RedirectUri ?? throw new ArgumentNullException(nameof(opciones.RedirectUri), "Falta redirectUri");
        urlServerApi = opciones.UrlServerApi ?? "https://api.cdmisiones.net.ar";
        urlServerAuth = opciones.UrlServerAuth ?? "https://auth.cdmisiones.net.ar";
        this.apiKey = opciones.ApiKey;
    }
    static dynamic Deserialize(string jsonString)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        var jsonDocument = JsonDocument.Parse(jsonString);
        var dictionary = new Dictionary<string, object>();

        foreach (var property in jsonDocument.RootElement.EnumerateObject())
        {
            dictionary[property.Name] = DeserializeElement(property.Value);
        }

        return dictionary;
    }

    static dynamic DeserializeElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dictionary = new Dictionary<string, object>();

                foreach (var property in element.EnumerateObject())
                {
                    dictionary[property.Name] = DeserializeElement(property.Value);
                }

                return dictionary;

            case JsonValueKind.Array:
                var list = new List<object>();

                foreach (var item in element.EnumerateArray())
                {
                    list.Add(DeserializeElement(item));
                }

                return list;

            case JsonValueKind.String:
                return element.GetString()!;

            case JsonValueKind.Number:
                // Handle other numeric types as needed (e.g., GetInt32, GetDouble, etc.)
                return element.GetInt16();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
            default:
                return null!;
        }
    }


    public async Task<CDMApiResponse> ApiAutorizaAsync(string keyLogin, string idSesion, string origenUri = "/")
    {

        var rta = new CDMApiResponse
        {
            status = 400,
            inStatus = 400,
            data = new Dictionary<dynamic, dynamic?> { { "Redirect", null } },
            msg = "Error Inesperado"
        };
        try
        {
            var handler = new HttpClientHandler()
            {
                AllowAutoRedirect = false
            };


            using (HttpClient httpClient = new HttpClient(handler))
            {
                var response = await httpClient.PostAsync($"{this.urlServerAuth}/auth/autoriza", new FormUrlEncodedContent(new Dictionary<string, string?>
                {
                    { "clientId", this.clientId },
                    { "clientSecret", this.clientSecret },
                    { "redirectUri", this.redirectUri },
                    { "origenUri", origenUri },
                    { "idSesion", idSesion },
                    { "keyLogin", keyLogin }
                }));

                if (response.StatusCode == System.Net.HttpStatusCode.Redirect)
                {
                    rta.status = 200;
                    rta.inStatus = 200;
                    rta.msg = "redireccionado";
                    rta.data["Redirect"] = response.Headers.Location?.ToString();
                }
                else
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var data = Deserialize(result);
                    //var data = JsonSerializer.Deserialize<CDMApiResponse>(result);
                    rta.inStatus = data["inStatus"] ?? 400;
                    rta.msg = data["msgStatus"];
                }
            }
        }
        catch (Exception)
        {
            return rta;
        }
        return rta;
    }

    public async Task<CDMApiResponse> api_getToken(string codigoAutorizacion)
    {
        var rta = new CDMApiResponse
        {
            status = 400,
            inStatus = 400,
            data = new Dictionary<dynamic, dynamic?> { { "token", null } },
            msg = "Error Inesperado"
        };
        try
        {
            using (HttpClient httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsync($"{this.urlServerAuth}/auth/token", new FormUrlEncodedContent(new Dictionary<string, string?>
                {
                    { "codigo", codigoAutorizacion },
                }));
                var result = await response.Content.ReadAsStringAsync();
                var data = Deserialize(result);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    rta.status = 200;
                    rta.inStatus = 200;
                    rta.msg = "ok";
                    rta.data["token"] = data["data"][0]["token"];
                }
                else
                {
                    rta.inStatus = data["inStatus"] ?? 400;
                    rta.msg = data["msgStatus"];
                }
            }
        }
        catch (Exception)
        {
            return rta;
        }
        return rta;
    }

    public async Task<CDMApiResponse> api_getPerfil(string token)
    {
        var rta = new CDMApiResponse
        {
            status = 400,
            inStatus = 400,
            data = new Dictionary<dynamic, dynamic?> { },
            msg = "Error Inesperado"
        };
        try
        {
            if (token != "" && token != null)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("token", token);
                    var response = await httpClient.GetAsync($"{this.urlServerApi}/api/coleccion/getPerfilColeccion");
                    var result = await response.Content.ReadAsStringAsync();
                    var data = Deserialize(result);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        rta.status = 200;
                        rta.inStatus = 200;
                        rta.msg = "ok";
                        rta.data = data["data"]?[0];
                    }
                    else
                    {
                        rta.inStatus = data["inStatus"] ?? 400;
                        rta.msg = data["msgStatus"];
                    }
                }
            }
        }
        catch (Exception)
        {
            return rta;
        }
        return rta;
    }

    public async Task<CDMApiResponse> api_verificaSesionToken(string token)
    {
        var rta = new CDMApiResponse
        {
            status = 400,
            inStatus = 400,
            data = new Dictionary<dynamic, dynamic?> { },
            msg = "Error Inesperado"
        };
        try
        {
            if (token != "" && token != null)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("token", token);
                    var response = await httpClient.GetAsync($"{this.urlServerAuth}/auth/verificaSesion");
                    var result = await response.Content.ReadAsStringAsync();
                    var data = Deserialize(result);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        rta.status = 200;
                        rta.inStatus = 200;
                        rta.msg = "ok";
                        rta.data = data["data"]?[0];
                    }
                    else
                    {
                        rta.inStatus = data["inStatus"] ?? 400;
                        rta.msg = data["msgStatus"];
                    }
                }
            }
        }
        catch (Exception)
        {
            return rta;
        }
        return rta;
    }

    public async Task<CDMApiResponse> api_cerrarSesion(string token)
    {
        var rta = new CDMApiResponse
        {
            status = 400,
            inStatus = 400,
            data = new Dictionary<dynamic, dynamic?> { },
            msg = "Error Inesperado"
        };
        try
        {
            if (token != "" && token != null)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("token", token);
                    var response = await httpClient.GetAsync($"{this.urlServerApi}/api/coleccion/cerrarSesionColeccion");
                    var result = await response.Content.ReadAsStringAsync();
                    var data = Deserialize(result);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        rta.status = 200;
                        rta.inStatus = 200;
                        rta.msg = data["msgStatus"];
                    }
                    else
                    {
                        rta.inStatus = data["inStatus"] ?? 400;
                        rta.msg = data["msgStatus"];
                    }
                }
            }
        }
        catch (Exception)
        {
            return rta;
        }
        return rta;
    }

    public async Task<CDMApiResponse> api_getSolicitud(string token, string solicitud)
    {
        var rta = new CDMApiResponse
        {
            status = 400,
            inStatus = 400,
            data = new Dictionary<dynamic, dynamic?> { },
            msg = "Error Inesperado"
        };
        try
        {
            if (token != "" && token != null)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("token", token);
                    var response = await httpClient.GetAsync($"{this.urlServerAuth}/solicitud/{solicitud}");
                    var result = await response.Content.ReadAsStringAsync();
                    var data = Deserialize(result);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        rta.status = 200;
                        rta.inStatus = 200;
                        rta.msg = "ok";
                        rta.data = data["data"]?[0];
                    }
                    else
                    {
                        rta.inStatus = data["inStatus"] ?? 400;
                        rta.msg = data["msgStatus"];
                    }
                }
            }
        }
        catch (Exception)
        {
            return rta;
        }
        return rta;
    }

    public async Task<CDMApiResponse> api_validaOTP(string token, string origenUri)
    {
        var rta = new CDMApiResponse
        {
            status = 400,
            inStatus = 400,
            data = new Dictionary<dynamic, dynamic?> { { "Redirect", null } },
            msg = "Error Inesperado"
        };
        try
        {
            if (token != "" && token != null)
            {
                var handler = new HttpClientHandler()
                {
                    AllowAutoRedirect = false
                };
                using (HttpClient httpClient = new HttpClient(handler))
                {
                    httpClient.DefaultRequestHeaders.Add("token", token);
                    httpClient.DefaultRequestHeaders.Add("origenuri", origenUri);
                    httpClient.DefaultRequestHeaders.Add("apikey", this.apiKey?["otp"]);
                    var response = await httpClient.GetAsync($"{this.urlServerAuth}/otp/valida");
                    var result = await response.Content.ReadAsStringAsync();
                    if (response.StatusCode == System.Net.HttpStatusCode.Redirect)
                    {
                        rta.status = 200;
                        rta.inStatus = 200;
                        rta.msg = "redireccionado";
                        rta.data["Redirect"] = response.Headers.Location?.ToString();
                    }
                    else
                    {
                        var data = Deserialize(result);
                        rta.inStatus = data["inStatus"] ?? 400;
                        rta.msg = data["msgStatus"];
                    }
                }
            }
        }
        catch (Exception)
        {
            return rta;
        }
        return rta;
    }

    public async Task<CDMApiResponse> api_validaFaceTec(string token, string origenUri)
    {
        var rta = new CDMApiResponse
        {
            status = 400,
            inStatus = 400,
            data = new Dictionary<dynamic, dynamic?> { { "Redirect", null } },
            msg = "Error Inesperado"
        };
        try
        {            
            if (token != "" && token != null)
            {
                var handler = new HttpClientHandler()
                {
                    AllowAutoRedirect = false
                };
                using (HttpClient httpClient = new HttpClient(handler))
                {
                    httpClient.DefaultRequestHeaders.Add("token", token);
                    httpClient.DefaultRequestHeaders.Add("origenuri", origenUri);
                    httpClient.DefaultRequestHeaders.Add("apikey", this.apiKey?["facetec"]);
                    var response = await httpClient.GetAsync($"{this.urlServerAuth}/facetec/valida");
                    var result = await response.Content.ReadAsStringAsync();
                    if (response.StatusCode == System.Net.HttpStatusCode.Redirect)
                    {
                        rta.status = 200;
                        rta.inStatus = 200;
                        rta.msg = "redireccionado";
                        rta.data["Redirect"] = response.Headers.Location?.ToString();
                    }
                    else
                    {
                        var data = Deserialize(result);
                        rta.inStatus = data["inStatus"] ?? 400;
                        rta.msg = data["msgStatus"];
                    }
                }
            }
        }
        catch (Exception)
        {
            return rta;
        }
        return rta;
    }

}



public class CDMApiOperacionesOpciones
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? RedirectUri { get; set; }
    public string? UrlServerApi { get; set; }
    public string? UrlServerAuth { get; set; }
    public Dictionary<string, dynamic>? ApiKey { get; set; }
}




public class CDMApiResponse
{
    public int? status { get; set; }
    public int? inStatus { get; set; }
    public dynamic? data { get; set; }
    public string? msg { get; set; }
    public string? msgStatus { get; set; }
}

public class CDMRequest
{
    public string? idColeccion { get; set; }
    public int? status { get; set; }
    public Dictionary<string, dynamic>? data { get; set; }
    public string? msg { get; set; }
    public string? token { get; set; }
}

public class CDMOpcionesMiddleware
{
    public string? rutaError { get; set; }
    public bool? ventanaVida { get; set; }
    public string[]? rutas { get; set; }
}
}
