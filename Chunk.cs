using Godot;
using System;
using System.Collections.Generic;

public partial class Chunk : StaticBody3D
{
	// Array 3D para armazenar os IDs dos voxels (byte é suficiente para IDs até 255)
	private byte[,,] voxels = new byte[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];
	
	internal MeshInstance3D visualMesh; //Variável para a malha de renderização
	
	// O gerador de ruído do Godot
	private FastNoiseLite noise = new FastNoiseLite();
	
	// Variáveis para construir a malha
	private List<Vector3> vertices = new List<Vector3>();
	private List<int> indices = new List<int>();
	private List<Vector3> normals = new List<Vector3>();
	private List<Vector2> uvs = new List<Vector2>();

	// Posição do chunk no mundo (em coordenadas de chunk, ex: (0, 0, 0))
	public Vector3 PositionInChunks;

	public void Initialize(Vector3 pos, int seed, Material material)
	{
		PositionInChunks = pos;
		
		// Configuração básica do ruído
		noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
		noise.Seed = seed; // Semente aleatória simples
		noise.Frequency = 0.005f; // Ajuste a frequência para o tamanho do mundo
		
		GenerateVoxelData();
		//cria o materical o CreateMesh
		CreateMesh(material);
	}

	// 1. Geração de Dados
	private void GenerateVoxelData()
	{
		// Coordenada global inicial do chunk
		int startX = (int)PositionInChunks.X * VoxelData.ChunkWidth;
		int startZ = (int)PositionInChunks.Z * VoxelData.ChunkWidth;

		for (int x = 0; x < VoxelData.ChunkWidth; x++)
		{
			for (int z = 0; z < VoxelData.ChunkWidth; z++)
			{
				// Calcula as coordenadas globais para o ruído
				float worldX = startX + x;
				float worldZ = startZ + z;

				// Gera a altura do terreno (Y)
				float noiseValue = noise.GetNoise2D(worldX, worldZ);
				
				// Normaliza e escala o valor do ruído para a altura do chunk
				// Ex: Altura entre 32 e 96
				int terrainHeight = (int)Mathf.Lerp(32, 96, (noiseValue + 1) / 2);

				for (int y = 0; y < VoxelData.ChunkHeight; y++)
				{
					//Calcula a altura global do Voxel
					int worldY = (int)PositionInChunks.Y * VoxelData.ChunkHeight+y;
					
					// Preenche os voxels
					byte voxelType = VoxelData.BL_AIR;
					
					if (worldY < terrainHeight)
					{
						// A) Estamos abaixo do nível da superfície: é um bloco sólido.
						
						// B) LÓGICA DE CAMADAS:
						
						// 1. Superfície (Grama)
						if (worldY == terrainHeight - 1)
						{
							voxelType = VoxelData.BL_GRASS;
						}
						// 2. Sub-superfície (Terra)
						else if (worldY >= terrainHeight - 4) // Camada de 3 blocos de terra (altura - 4, -3, -2)
						{
							voxelType = VoxelData.BL_DIRT;
						}
						// 3. Subterrâneo (Pedra)
						else
						{
							voxelType = VoxelData.BL_STONE;
						}
					}

					voxels[x, y, z] = voxelType;
				}
			}
		}
	}
	
	// Função para obter o tipo de voxel em uma coordenada local
	private byte GetVoxel(int x, int y, int z)
	{
		// Se estiver dentro dos limites do chunk, retorna o voxel
		if (x >= 0 && x < VoxelData.ChunkWidth && y >= 0 && y < VoxelData.ChunkHeight && z >= 0 && z < VoxelData.ChunkWidth)
		{
			return voxels[x, y, z];
		}
		
		// Se estiver fora, retorna ar (ou você precisaria de lógica para buscar em chunks vizinhos)
		return VoxelData.BL_AIR;
	}
	//Magia negra
	// 2. Criação da Malha (Face Culling)
	// Chunk.cs -  CreateMesh
	public void CreateMesh(Material material)
	{

		foreach (Node child in GetChildren())
		{
			if (child is MeshInstance3D || child is CollisionShape3D)
			{
				child.QueueFree(); // Usa QueueFree para remoção segura
			}
		}
		
		// Zera as listas para a nova construção
		vertices.Clear();
		indices.Clear();
		normals.Clear();
		uvs.Clear();

		for (int x = 0; x < VoxelData.ChunkWidth; x++)
		{
			for (int y = 0; y < VoxelData.ChunkHeight; y++)
			{
				for (int z = 0; z < VoxelData.ChunkWidth; z++)
				{
					if (GetVoxel(x, y, z) != VoxelData.BL_AIR)
					{
						AddVoxelDataToMesh(new Vector3(x, y, z));
					}
				}
			}
		}

		if (vertices.Count == 0)
		{
			// Se o chunk estiver vazio (só ar), garante que a malha seja nula
			visualMesh = null;
			return; 
		}
		
		// 2. CRIAÇÃO DA NOVA MALHA E ATRIBUIÇÃO AO MEMBRO DA CLASSE
		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)RenderingServer.ArrayType.Max);
		
		arrays[(int)RenderingServer.ArrayType.Vertex] = vertices.ToArray();
		arrays[(int)RenderingServer.ArrayType.Index] = indices.ToArray();
		arrays[(int)RenderingServer.ArrayType.Normal] = normals.ToArray();
		arrays[(int)RenderingServer.ArrayType.TexUV] = uvs.ToArray();
		
		var arrayMesh = new ArrayMesh();
		arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		
		// ATRIBUIÇÃO CORRETA: Usa a variável membro 'visualMesh'
		// Não use 'var' aqui, pois isso criaria uma variável LOCAL.
		visualMesh = new MeshInstance3D();
		visualMesh.Mesh = arrayMesh;
		
		if (material != null)
		{
			visualMesh.MaterialOverride = material;
		}
		AddChild(visualMesh); // Adiciona a nova malha

		// 3. CRIAÇÃO DA COLISÃO
		var collisionShape = new CollisionShape3D();
		var shape = arrayMesh.CreateTrimeshShape(); 
		collisionShape.Shape = shape;

		AddChild(collisionShape); // Adiciona a nova colisão
		SetCollisionLayerValue(1, true); 
		SetCollisionMaskValue(1, true);
	}
	
	private Vector2[] GetTextureUVs(byte blockID, int faceIndex)
	{
		float offset = VoxelData.NormalizedTileSize; 
		
		// 1. OBTÉM AS COORDENADAS BASE
		Vector2 tileCoords = VoxelData.TextureAtlas[blockID, faceIndex];

		float x = tileCoords.X * offset;
		float y = tileCoords.Y * offset;
		
		// 2. DEFINE AS COORDENADAS UV BASE DA TILE
		// P1: Inferior Esquerdo (BL)
		Vector2 P1 = new Vector2(x,          y + offset); 
		// P2: Inferior Direito (BR)
		Vector2 P2 = new Vector2(x + offset, y + offset); 
		// P3: Superior Direito (TR)
		Vector2 P3 = new Vector2(x + offset, y);         
		// P4: Superior Esquerdo (TL)
		Vector2 P4 = new Vector2(x,          y);         

		Vector2[] uvs = new Vector2[4];

		// ESTA TUDO ERRADO AQUI EM BAIXO, TUDO MESMO, MAS ESTA FUNCIONANDO D:
		
		// Padrão: (P1, P2, P3, P4)
		Vector2[] standardUVs = new Vector2[] { P1, P2, P3, P4 };
		
		// Rotação/Inversão (270°/90° Anti-horário) - Necessário para Z+ e X-
		Vector2[] rotation1 = new Vector2[] { P2, P3, P4, P1 };
		
		// Inversão 180° (Comum para Z- e X+)
		Vector2[] rotation2 = new Vector2[] { P3, P4, P1, P2 };

		// Correção Z- (para outros blocos)
		Vector2[] invertedZNegativeUVs = new Vector2[] { P2, P1, P4, P3 };
		
		// A lógica de Grama só se aplica a faces laterais (0, 1, 4, 5)
		bool isGrassLateralFace = (blockID == VoxelData.BL_GRASS);
		
		switch (faceIndex)
		{
			case 2: // Y+ (Cima)
			case 3: // Y- (Baixo)
				// Topo e Base: usar ordem padrão para todos os blocos.
				uvs = standardUVs;
				break;
				
			case 0: // Z+ (Frente)
				uvs = standardUVs;
				break;

			case 1: // Z- (Trás)
				uvs = isGrassLateralFace ? rotation1 : standardUVs;
				break;

			case 4: // X+ (Direita)
				uvs = isGrassLateralFace ? rotation1 : standardUVs;
				break;
				
			case 5: // X- (Esquerda)
				uvs = isGrassLateralFace ? rotation1 : standardUVs;
				break;
				
			default:
				uvs = standardUVs;
				break;
		}
		
		return uvs;
	}
	
	// Chunk.cs - Função AddVoxelDataToMesh - as coisas voltam ao normal

	private void AddVoxelDataToMesh(Vector3 pos)
	{
		byte voxelType = voxels[(int)pos.X, (int)pos.Y, (int)pos.Z]; // <-- Pegar o tipo de bloco aqui!
		// Itera sobre as 6 faces do cubo  
		for (int i = 0; i < 6; i++) // i: Face index (0 a 5)
		{
			int neighborX = (int)pos.X + (int)VoxelData.faceChecks[i].X;
			int neighborY = (int)pos.Y + (int)VoxelData.faceChecks[i].Y;
			int neighborZ = (int)pos.Z + (int)VoxelData.faceChecks[i].Z;
			// ... (Verifica se o vizinho é ar)

			if (GetVoxel(neighborX, neighborY, neighborZ) == VoxelData.BL_AIR)
			{
				int vertexIndex = vertices.Count; 
				
				// 1. OBTENHA AS UVs CORRETAS PARA ESTA FACE E ESTE TIPO DE BLOCO
				Vector2[] faceUVs = GetTextureUVs(voxelType, i);

				// Adiciona os 4 vértices da face
				for (int j = 0; j < 4; j++) // j: Vértice index da face (0 a 3)
				{
					int vertIndex = VoxelData.VoxelFaceVerts[i * 4 + j]; 
					
					// Adiciona o vértice global (VoxelVerts[vertIndex] + pos)
					vertices.Add(VoxelData.VoxelVerts[vertIndex] + pos);
					
					normals.Add(VoxelData.faceChecks[i]); 
					uvs.Add(faceUVs[j]); 
				}

				// Adiciona os 6 índices (2 triângulos)
				// Os 4 vértices adicionados acima estão em (vertexIndex + 0, 1, 2, 3)

				// Triângulo 1 (0, 3, 2)
				indices.Add(vertexIndex + 0);
				indices.Add(vertexIndex + 3);
				indices.Add(vertexIndex + 2);

				// Triângulo 2 (0, 2, 1)
				indices.Add(vertexIndex + 0);
				indices.Add(vertexIndex + 2);
				indices.Add(vertexIndex + 1);
			}
		}
	}
	
	//Quebrar blocos
	public void SetVoxel(int x, int y, int z, byte type)
	{
		// Verifica se as coordenadas estão dentro dos limites do chunk
		if (x >= 0 && x < VoxelData.ChunkWidth &&
			y >= 0 && y < VoxelData.ChunkHeight &&
			z >= 0 && z < VoxelData.ChunkWidth)
		{
			voxels[x, y, z] = type;
			
			// Reconstrói a malha
			CreateMesh(visualMesh.MaterialOverride); // Passe o material atual
			
			// --- IMPORTANTE: ATUALIZAR CHUNKS VIZINHOS ---
			
			// Se a modificação ocorreu na borda, o chunk vizinho também deve ser reconstruído
			// para fechar ou abrir o buraco.
			
			// A lógica de reconstrução de vizinhos deve ser tratada no World.cs 
			// após a chamada de SetVoxel, para garantir que você tenha acesso 
			// a todos os chunks pelo dicionário 'chunks'.
		}
	}
}
