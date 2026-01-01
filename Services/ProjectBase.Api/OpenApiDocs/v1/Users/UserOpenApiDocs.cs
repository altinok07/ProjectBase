namespace ProjectBase.Api.OpenApiDocs.v1.Users;

public static class UserOpenApiDocs
{
    public static class Register
    {
        public const string Summary = "Kullanıcı oluşturma işlemi";
        public const string Description = """
        JWT Bearer token gerektirir.

        - **Name** (`string`): zorunlu, en fazla 64 karakter
        - **Surname** (`string`): zorunlu, en fazla 64 karakter
        - **Mail** (`string`): zorunlu, en fazla 64 karakter, geçerli e-posta formatı
        - **Password** (`string`): zorunlu, en az 8 karakter
        """;
    }
}


