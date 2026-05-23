FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY AssistedEcommerce.Api.csproj .
RUN dotnet restore AssistedEcommerce.Api.csproj
COPY . .
RUN dotnet publish AssistedEcommerce.Api.csproj -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update \
    && apt-get install -y --no-install-recommends tesseract-ocr tesseract-ocr-eng \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/out .
ENV ASPNETCORE_ENVIRONMENT=Production
# Listen on Railway/Render PORT (set in Program.cs); do not hardcode 8080 here.
ENTRYPOINT ["dotnet", "AssistedEcommerce.Api.dll"]
