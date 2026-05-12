# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EmployeeTrainer.csproj .
RUN dotnet restore EmployeeTrainer.csproj

COPY . .
RUN dotnet publish EmployeeTrainer.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
USER root

RUN mkdir -p /data /app/wwwroot/uploads

COPY --from=build /app/publish .
COPY .docker/docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__DefaultConnection=Data Source=/data/employeetrainer.db

EXPOSE 8080

ENTRYPOINT ["/docker-entrypoint.sh"]
