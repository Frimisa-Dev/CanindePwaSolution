@echo off
cls

:: Mapeia o caminho UNC para uma unidade temporária
pushd \\Eduardo\c\Desenvolvimento\VisualStudio\canindePwaSolution
if errorlevel 1 (
    echo Erro ao acessar \\Eduardo\c\Desenvolvimento\VisualStudio\canindePwaSolution
    pause
    exit /b
)

:: Define o diretório de destino
set DESTINO=E:\Desenvolvimento\VisualStudio\canindePwaSolution

:: Copia as pastas usando xcopy
xcopy *.* "%DESTINO%" /y /s /e /d


::xcopy ScriptMySql\*.* "%DESTINO%\ScriptMySql" /y /s /e /d

:: Retorna ao diretório original e libera a unidade temporária
popd

echo Backup concluído com sucesso!
pause