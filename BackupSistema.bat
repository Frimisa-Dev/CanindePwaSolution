@echo off
cls

:: Mapeia o caminho UNC para uma unidade temporária
pushd \\Eduardo\c\Desenvolvimento\VisualStudio\falcaoPwaSolution
if errorlevel 1 (
    echo Erro ao acessar \\Eduardo\c\Desenvolvimento\VisualStudio\falcaoPwaSolution
    pause
    exit /b
)

:: Define o diretório de destino
set DESTINO=E:\Desenvolvimento\VisualStudio\falcaoPwaSolution

:: Copia as pastas usando xcopy
xcopy *.* "%DESTINO%" /y /s /e /d


::xcopy ScriptMySql\*.* "%DESTINO%\ScriptMySql" /y /s /e /d

:: Retorna ao diretório original e libera a unidade temporária
popd

echo Backup concluído com sucesso!
pause