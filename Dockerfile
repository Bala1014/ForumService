# Restore is a separate layer keyed only on the project files, so a code-only change reuses the
# cached package restore instead of re-downloading every dependency.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ForumService.slnx ./
COPY src/Racinglazing.Forum.Api/Racinglazing.Forum.Api.csproj src/Racinglazing.Forum.Api/
COPY src/Racinglazing.Forum.Application/Racinglazing.Forum.Application.csproj src/Racinglazing.Forum.Application/
COPY src/Racinglazing.Forum.Domain/Racinglazing.Forum.Domain.csproj src/Racinglazing.Forum.Domain/
COPY src/Racinglazing.Forum.Infrastructure/Racinglazing.Forum.Infrastructure.csproj src/Racinglazing.Forum.Infrastructure/
RUN dotnet restore src/Racinglazing.Forum.Api/Racinglazing.Forum.Api.csproj

COPY . .
RUN dotnet publish src/Racinglazing.Forum.Api/Racinglazing.Forum.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Runs as a non-root user. Cloud Run does not require it, but a container that cannot write to
# its own filesystem is one less thing an exploited dependency can do.
RUN adduser --disabled-password --gecos "" --uid 64198 forumservice \
    && chown -R forumservice /app
USER forumservice

COPY --from=build --chown=forumservice /app/publish .

# Cloud Run supplies PORT; this default keeps local `docker run` working unchanged.
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "Racinglazing.Forum.Api.dll"]
