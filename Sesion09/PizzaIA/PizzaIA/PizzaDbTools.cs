using System.ComponentModel;

namespace PizzaIA;

public class PizzaDbTools
{
    private static readonly string _schema = File.ReadAllText("pizza-db.md");

    [Description(
        "Devuelve el diccionario de datos de la Pizzeria 'Mi Pizza'."
        )]
    public string ObtenerEsquema() => _schema;
}