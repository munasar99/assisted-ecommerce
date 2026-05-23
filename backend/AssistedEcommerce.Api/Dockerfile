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
# OCR: copy eng.traineddata for .NET Tesseract + verify CLI
RUN mkdir -p /app/tessdata \
    && find /usr/share -type f -name 'eng.traineddata' -exec cp -f {} /app/tessdata/ \; \
    && tesseract --version
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AssistedEcommerce.Api.dll"]
