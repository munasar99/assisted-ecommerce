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
# .NET Tesseract looks for /app/tessdata/eng.traineddata (NuGet may not copy on Linux publish).
RUN mkdir -p /app/tessdata \
    && (test -f /app/tessdata/eng.traineddata \
        || cp /usr/share/tesseract-ocr/*/tessdata/eng.traineddata /app/tessdata/ 2>/dev/null \
        || cp /usr/share/tesseract-ocr/4.00/tessdata/eng.traineddata /app/tessdata/ 2>/dev/null \
        || true)
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AssistedEcommerce.Api.dll"]
