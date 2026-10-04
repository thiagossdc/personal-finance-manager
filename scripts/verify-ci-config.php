<?php

declare(strict_types=1);

/**
 * Validação estática do workflow e da solução de CI.
 *
 * Existe porque os erros mais comuns aqui (indentação quebrada, tabulação,
 * projeto MAUI reintroduzido por engano) só apareceriam no runner do GitHub.
 * Requer apenas PHP — sem dependência de PyYAML.
 */

$root = dirname(__DIR__);
$workflow = $root . '/.github/workflows/ci.yml';
$slnx = $root . '/PersonalFinanceManager.CI.slnx';

$failures = [];

echo "== YAML: indentação e caracteres =\n";

if (!is_file($workflow)) {
    fwrite(STDERR, "workflow não encontrado\n");

    exit(1);
}

$yaml = file_get_contents($workflow);

if (str_contains($yaml, "\t")) {
    $failures[] = 'YAML contém tabulação (proibido pelo parser)';
}

if (str_contains($yaml, "\r")) {
    $failures[] = 'YAML tem quebras de linha CRLF';
}

// Cada passo precisa ter "name" ou "uses"; sem isso o step é inválido.
preg_match_all('/^\s{6}- (.*)$/m', $yaml, $steps);
echo 'passos encontrados: ', count($steps[1]), "\n";

foreach ($steps[1] as $step) {
    if (!str_contains($step, 'name:') && !str_contains($step, 'uses:')) {
        $failures[] = "passo sem 'name' nem 'uses': " . trim($step);
    }
}

// O runner precisa ser Linux explícito, e o filtro antigo de testes não pode
// voltar: `FullyQualifiedName!~Maui` não exclui assemblies, só nomes de teste.
if (!str_contains($yaml, 'ubuntu-latest')) {
    $failures[] = 'runner não é ubuntu-latest';
}

if (str_contains($yaml, 'FullyQualifiedName!~Maui')) {
    $failures[] = 'filtro FullyQualifiedName!~Maui presente (não exclui o projeto MAUI)';
}

if (str_contains($yaml, 'dotnet restore PersonalFinanceManager.slnx')) {
    $failures[] = 'restore aponta para a solução completa (inclui o projeto MAUI)';
}

echo "\n== Solução de CI: projetos =\n";

if (!is_file($slnx)) {
    fwrite(STDERR, "PersonalFinanceManager.CI.slnx não encontrado\n");

    exit(1);
}

$xml = simplexml_load_file($slnx);
if ($xml === false) {
    fwrite(STDERR, "slnx inválido\n");

    exit(1);
}

$projects = [];
foreach ($xml->Folder->Project as $project) {
    $path = (string) $project['Path'];
    $projects[] = $path;
    echo ' - ', $path, "\n";
}

echo 'total: ', count($projects), "\n";

// O app MAUI não pode estar na solução de CI (exige macOS + Xcode).
foreach ($projects as $path) {
    if (str_ends_with($path, 'PersonalFinance.Maui.csproj')) {
        $failures[] = 'projeto MAUI (UI) presente na solução de CI';
    }
}

// Todo projeto referenciado pelo caminho precisa existir no disco.
foreach ($projects as $path) {
    if (!is_file($root . '/' . $path)) {
        $failures[] = "projeto inexistente: {$path}";
    }
}

// A suíte de testes do MAUI.Core continua válida: é net10.0 puro.
$temMauiCore = false;
foreach ($projects as $path) {
    if (str_ends_with($path, 'PersonalFinance.Maui.Core.csproj')) {
        $temMauiCore = true;
    }
}

if (!$temMauiCore) {
    $failures[] = 'PersonalFinance.Maui.Core (net10.0 puro) ausente — cobertura perdida';
}

echo "\n";

if ($failures !== []) {
    fwrite(STDERR, "FALHAS:\n");

    foreach ($failures as $failure) {
        fwrite(STDERR, "  - {$failure}\n");
    }

    exit(1);
}

echo "OK — workflow e solução de CI consistentes.\n";