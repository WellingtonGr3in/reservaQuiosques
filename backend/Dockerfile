# Imagem da API (.NET 9). Construída em duas etapas ("multi-stage"):
#   1. "build": usa a imagem com o SDK completo para compilar e publicar a API;
#   2. "runtime": copia só o resultado para uma imagem menor, que tem apenas o necessário para rodar.

# ---------- 1. Compilação ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Primeiro copia só os arquivos de projeto e restaura os pacotes NuGet. Enquanto os .csproj não mudarem,
# o Docker reaproveita esta etapa do cache e o build seguinte fica bem mais rápido.
COPY global.json Quiosques.sln ./
COPY src/Quiosques.Domain/Quiosques.Domain.csproj src/Quiosques.Domain/
COPY src/Quiosques.Application/Quiosques.Application.csproj src/Quiosques.Application/
COPY src/Quiosques.Infrastructure/Quiosques.Infrastructure.csproj src/Quiosques.Infrastructure/
COPY src/Quiosques.Api/Quiosques.Api.csproj src/Quiosques.Api/
RUN dotnet restore src/Quiosques.Api/Quiosques.Api.csproj

# Depois copia o código e publica a API (os testes não entram na imagem).
COPY src/ src/
RUN dotnet publish src/Quiosques.Api/Quiosques.Api.csproj -c Release -o /app --no-restore

# ---------- 2. Execução ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Idioma do container: datas e valores no log em português (ex.: 20/11/2026, 150,00).
ENV LANG=pt_BR.UTF-8

# Pasta das fotos enviadas pelo admin (volume no docker-compose.yml). Criada com dono "app",
# o usuário sem privilégios das imagens .NET, para a API conseguir gravar nela.
ENV Storage__ImagesPath=/app/uploads
RUN mkdir -p /app/uploads && chown app:app /app/uploads

# A API escuta na porta 8080 dentro do container (padrão das imagens .NET 8+).
EXPOSE 8080
ENTRYPOINT ["dotnet", "Quiosques.Api.dll"]
