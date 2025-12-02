using Godot;
using System;
using System.Collections.Generic;

public partial class World : Node3D
{
	private Dictionary<Vector3, Chunk> chunks = new Dictionary<Vector3, Chunk>();
	
	[Export] public Material VoxelMaterial; // Arraste seu Material para cá no Inspector
	
	private int WorldSeed;
	
	public override void _Ready()
	{
		WorldSeed = (int)GD.Randi();
		GenerateWorld();
	}

	private void GenerateWorld()
	{
		GD.Print($"Gerando mundo de {VoxelData.WorldSizeInChunks}x{VoxelData.WorldSizeInChunks} chunks...");
		
		// Itera sobre os chunks no plano XZ
		for (int x = 0; x < VoxelData.WorldSizeInChunks; x++)
		{
			for (int z = 0; z < VoxelData.WorldSizeInChunks; z++)
			{
				CreateChunk(new Vector3(x, 0, z));
			}
		}
		
		GD.Print("Geração de mundo concluída.");
	}

	private void CreateChunk(Vector3 chunkCoords)
	{
		var chunk = new Chunk();
		
		// Calcula a posição do mundo em blocos (chunkCoords * ChunkWidth)
		Vector3 worldPosition = new Vector3(
			chunkCoords.X * VoxelData.ChunkWidth,
			0, // Chunks só se movem em X e Z
			chunkCoords.Z * VoxelData.ChunkWidth
		);
		
		chunk.Translate(worldPosition);
		
		// Inicializa e gera os dados e a malha
		chunk.Initialize(chunkCoords, WorldSeed, VoxelMaterial);
		
		AddChild(chunk);
		chunks.Add(chunkCoords, chunk);
	}
	
	public void ModifyVoxel(Vector3 hitPoint, Vector3 normal, bool isBreaking)
	{
		// 1. Determina a Posição Global do Voxel
		Vector3 voxelPosGlobal;
		
		if (isBreaking)
		{
			// Ao quebrar, recuamos ligeiramente do ponto de colisão na direção oposta à normal
			// para garantir que estamos DENTRO do bloco atingido.
		voxelPosGlobal = hitPoint - normal * 0.01f;
		}
		else // Colocando um bloco
		{
			// Ao colocar, avançamos ligeiramente na direção da normal
			// para garantir que estamos no ESPAÇO VAZIO adjacente.
			voxelPosGlobal = hitPoint + normal * 0.01f;
		}
		
		// 2. Arredonda a posição para a coordenada do voxel.
		voxelPosGlobal = new Vector3(
			Mathf.Floor(voxelPosGlobal.X), 
			Mathf.Floor(voxelPosGlobal.Y), 
			Mathf.Floor(voxelPosGlobal.Z)
		);
		
		// 3. Encontra o chunk
		Chunk targetChunk = GetChunkFromWorldPos(voxelPosGlobal);
		
		if (targetChunk != null)
		{
			// 4. Converte a posição global para a posição LOCAL do chunk
			int x = (int)(voxelPosGlobal.X - targetChunk.GlobalPosition.X);
			int y = (int)voxelPosGlobal.Y; // Assumindo Y=0 para o chunk
			int z = (int)(voxelPosGlobal.Z - targetChunk.GlobalPosition.Z);
			
			byte newType = isBreaking ? VoxelData.BL_AIR : VoxelData.BL_DIRT; // Use o bloco desejado

			// 5. Modifica e reconstrói o chunk
			targetChunk.SetVoxel(x, y, z, newType);

			// Lógica para reconstruir vizinhos se a modificação foi na borda
			if (x == 0 || x == VoxelData.ChunkWidth - 1 || z == 0 || z == VoxelData.ChunkWidth - 1)
			{
				// A reconstrução de vizinhos é complexa, pois depende da direção.
				// Vamos simplificar e reconstruir o chunk em todas as direções para teste.
				
				// Exemplo Simples (Requer otimização futura!!!):
				for (int i = 0; i < 6; i++)
				{
					Vector3 neighborOffset = VoxelData.faceChecks[i] * VoxelData.ChunkWidth;
					Vector3 neighborCoords = targetChunk.PositionInChunks + neighborOffset;
					
					if (chunks.TryGetValue(neighborCoords, out Chunk neighborChunk))
					{
						// Força a reconstrução do vizinho.
						neighborChunk.CreateMesh(neighborChunk.visualMesh.MaterialOverride);
					}
				}
			}
		}
	}
	private Chunk GetChunkFromWorldPos(Vector3 worldPos)
	{
		// 1. Converte a posição mundial para a coordenada do chunk (0, 0, 0), (1, 0, 0), etc.
		// Usamos Mathf.Floor para lidar corretamente com coordenadas negativas se o mundo se expandir.
		int chunkX = (int)Mathf.Floor(worldPos.X / VoxelData.ChunkWidth);
		int chunkZ = (int)Mathf.Floor(worldPos.Z / VoxelData.ChunkWidth);
			
		// 2. Cria a chave Vector3 para buscar no dicionário 'chunks'
		// A coordenada Y é 0 porque o mundo só é dividido em XZ.
		Vector3 chunkCoords = new Vector3(chunkX, 0, chunkZ);
			
		// 3. Busca o chunk no dicionário
		if (chunks.TryGetValue(chunkCoords, out Chunk chunk))
		{
			return chunk;
		}
		return null; // Retorna null se o chunk não foi carregado/gerado
	}
}
