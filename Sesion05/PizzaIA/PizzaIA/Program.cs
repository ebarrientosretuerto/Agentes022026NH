using Microsoft.Extensions.AI;
using System.Runtime.InteropServices.Marshalling;

var builder = WebApplication.CreateBuilder(args);

var openAICliente = new OpenAICliente
{
    Temperature = 0.7f, //Creatividad Moderada
    TopP = 0.9f,        //Probabilidad de respuesta
    TopK = 50,          //Considerar los 50 tokens más probables
    StreamDelay = 50    //50ms entre chunks para efecto de escritura
};