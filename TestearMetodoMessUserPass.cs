using Xunit;

namespace RouterUpgrade
{
    public class TestearMetodoMessUserPass
    {
        [Fact]
        public void Usuario()
        {
            const string usuario = "1234";

            var resultado = MetodoMessUserPass.MessUserPass(usuario);

            Assert.Equal(".-,+", resultado);
        }

        [Fact]
        public void Pass()
        {
            const string usuario = "GAxDSHuV";

            var resultado = MetodoMessUserPass.MessUserPass(usuario);

            Assert.Equal("X^g[LWjI", resultado);
        }

    }
}
