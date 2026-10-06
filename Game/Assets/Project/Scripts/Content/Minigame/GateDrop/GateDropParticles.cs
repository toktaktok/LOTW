using UnityEngine;

namespace Project.Scripts.Content.Minigame.GateDrop
{
    /// <summary>
    /// 1픽셀 입자 풀 (핀 불똥, 착지 색종이, 꽝 연기). 위치는 스테이지 픽셀 격자에 맞춰 그립니다.
    /// 풀이 다 차면 가장 오래된 입자를 다시 씁니다.
    /// </summary>
    public class GateDropParticles
    {
        private struct Particle
        {
            public Vector2 position;
            public Vector2 velocity;
            public Color color;
            public float gravity;
            public float drag;
            public float life;
            public float age;
        }

        private readonly GateDropLayout _layout;
        private readonly SpriteRenderer[] _renderers;
        private readonly Particle[] _particles;
        private int _next;

        public GateDropParticles(GateDropLayout layout, SpriteRenderer[] renderers)
        {
            _layout = layout;
            _renderers = renderers;
            _particles = new Particle[renderers.Length];
            foreach(SpriteRenderer renderer in renderers)
                renderer.enabled = false;
        }

        /// <summary>핀에 맞은 불똥: 맞은 점에서 튕긴 반대쪽 위로 몇 개.</summary>
        public void Sparks(Vector2 origin, int direction)
        {
            int count = Random.Range(2, 5);
            for(int i = 0; i < count; i++)
            {
                float angle = Random.Range(30f, 80f);
                float side = direction == 0 ? (Random.value < 0.5f ? -1f : 1f) : -direction;
                Vector2 velocity = Polar(side > 0f ? angle : 180f - angle, Random.Range(28f, 60f));
                Color color = Color.Lerp(new Color(1f, 0.95f, 0.7f), new Color(1f, 0.75f, 0.3f), Random.value);
                Emit(origin, velocity, color, 220f, 2f, Random.Range(0.14f, 0.26f));
            }
        }

        /// <summary>착지 색종이: 출구에서 위로 터져 떨어짐. 음료 색과 흰색을 섞음.</summary>
        public void Confetti(Vector2 origin, Color color, int count)
        {
            for(int i = 0; i < count; i++)
            {
                Vector2 velocity = Polar(Random.Range(55f, 125f), Random.Range(45f, 95f));
                Color tint = i % 3 == 0 ? Color.white : Color.Lerp(color, Color.white, Random.Range(0f, 0.3f));
                Emit(origin + new Vector2(Random.Range(-2f, 2f), 0f), velocity, tint, 150f, 1.2f, Random.Range(0.55f, 0.9f));
            }
        }

        /// <summary>꽝: 회색 연기가 천천히 피어오름.</summary>
        public void Puff(Vector2 origin, Color color, int count)
        {
            for(int i = 0; i < count; i++)
            {
                Vector2 velocity = Polar(Random.Range(60f, 120f), Random.Range(8f, 22f));
                Color tint = Color.Lerp(color, Color.white, Random.Range(0.1f, 0.4f));
                Emit(origin + new Vector2(Random.Range(-3f, 3f), Random.Range(-1f, 2f)), velocity, tint, -6f, 1.5f, Random.Range(0.45f, 0.75f));
            }
        }

        public void Tick(float deltaTime)
        {
            for(int i = 0; i < _particles.Length; i++)
            {
                SpriteRenderer renderer = _renderers[i];
                if(!renderer.enabled)
                    continue;

                ref Particle particle = ref _particles[i];
                particle.age += deltaTime;
                if(particle.age >= particle.life)
                {
                    renderer.enabled = false;
                    continue;
                }

                particle.velocity.y -= particle.gravity * deltaTime;
                particle.velocity *= Mathf.Exp(-particle.drag * deltaTime);
                particle.position += particle.velocity * deltaTime;
                renderer.transform.localPosition = _layout.ToLocal(particle.position);

                // 수명의 마지막 40% 동안 깜빡이며 사라짐
                float remaining = 1f - particle.age / particle.life;
                Color color = particle.color;
                if(remaining < 0.4f)
                    color.a = (Mathf.FloorToInt(particle.age * 30f) & 1) == 0 ? remaining / 0.4f : 0f;
                renderer.color = color;
            }
        }

        private void Emit(Vector2 origin, Vector2 velocity, Color color, float gravity, float drag, float life)
        {
            int index = _next;
            _next = (_next + 1) % _particles.Length;
            _particles[index] = new Particle
            {
                position = origin,
                velocity = velocity,
                color = color,
                gravity = gravity,
                drag = drag,
                life = life,
                age = 0f
            };
            SpriteRenderer renderer = _renderers[index];
            renderer.enabled = true;
            renderer.color = color;
            renderer.transform.localPosition = _layout.ToLocal(origin);
        }

        private static Vector2 Polar(float degrees, float speed)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * speed;
        }
    }
}
