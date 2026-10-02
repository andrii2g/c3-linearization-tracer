FROM mcr.microsoft.com/dotnet/sdk:10.0 AS test
WORKDIR /repo
COPY . .
RUN bash scripts/test.sh
