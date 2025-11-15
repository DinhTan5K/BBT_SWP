FROM mcr.microsoft.com/mssql/server:2022-latest

ENV ACCEPT_EULA=Y
ENV MSSQL_SA_PASSWORD=Check123!
ENV MSSQL_PID=Developer

EXPOSE 1433

RUN mkdir -p /var/opt/mssql/data

CMD ["/opt/mssql/bin/sqlservr"]
