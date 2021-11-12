# Встановлення операційної системи
FROM centos:7.6.1810

# опис зображення 
LABEL name="composdev"
LABEL version="14.0"
LABEL operating_system="centos:7.6.1810"
LABEL environment="production"
LABEL maintainer="kalancha.artem@gmail.com"

ENV ACCEPT_EULA=Y 
ENV SA_PASSWORD=diplom_123123Aa
ENV ASPNETCORE_URLS=http://+:80

# встановлення бази даних MSSQL Server
RUN curl -o /etc/yum.repos.d/mssql-server.repo https://packages.microsoft.com/config/rhel/7/mssql-server-2017.repo
RUN yum install -y mssql-server
ENV PATH=${PATH}:/opt/mssql/bin
RUN mkdir -p /var/opt/mssql/data
RUN chmod -R g=u /var/opt/mssql /etc/passwd

# встановлення nodejs для фронтенду
RUN curl -sL https://rpm.nodesource.com/setup_10.x | bash -
RUN yum install -y nodejs

# встановлення компілятора .NET для запуску API
RUN rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm
RUN yum install -y dotnet-sdk-3.1

# Копіювання файлів в зображення та опис робочих портів веб-сервера
WORKDIR /app
EXPOSE 80
COPY ["command.sh", "command.sh"]

# Копіювання проектів для відновлення пакетів
WORKDIR /src
COPY ["API/API.csproj", "API/"]
COPY ["Infrastructures/Infrastructures.csproj", "Infrastructures/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Persistence/Persistence.csproj", "Persistence/"]

# Відновлення пакетів, вказання доступних портів для запуску АРІ та публікація додатку 
RUN dotnet restore "API/API.csproj"
COPY . .
WORKDIR "/src/API"
RUN dotnet publish "API.csproj" -c Release -o /app/publish

# Запуск серверів бази даних та API за допомогою Bash
WORKDIR /app
CMD sh command.sh
