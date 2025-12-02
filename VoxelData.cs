using Godot;
using System;

public static class VoxelData
{
	
	// Dimensões de um chunk (pedaço)
	public const int ChunkWidth = 16;
	public const int ChunkHeight = 128; // Altura máxima do mundo
	
	// Dimensões do mundo em chunks (WorldSizeInChunks = Quantidade de Chunk * 16 Blocos)
	public const int WorldSizeInChunks = 8; // 16 chunks * 16 blocos/chunk = 256 blocos
	
	// Dimensões do mundo em blocos (para referência)
	public const int WorldSizeInVoxels = WorldSizeInChunks * ChunkWidth;

	// IDs dos Blocos
	public const byte BL_AIR = 0;
	public const byte BL_GRASS = 1;
	public const byte BL_DIRT = 2;
	public const byte BL_STONE = 3;
	
		// Dimensões do Atlas (4x4 = 16 texturas no total)
	public const int TextureAtlasSizeInTiles = 4;
	public const float NormalizedTileSize = 1f / TextureAtlasSizeInTiles; // 1/8 = 0.125f
	
	
	public static readonly Vector2[,] TextureAtlas = new Vector2[4, 6]
	{
	// Índice 0 (BL_AIR) - Não usado.
	{ new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0) },
	
	// Índice 1 (BL_GRASS)
	{
		// Face Z+, Z-, X+, X- (Laterais) usam Lateral da Grama/Terra: (1, 0)
		new Vector2(1, 0), new Vector2(1, 0), 
		// Face Y+ (Topo) usa Topo da Grama: (0, 0)
		new Vector2(0, 0), 
		// Face Y- (Inferior) usa Terra: (2, 0)
		new Vector2(2, 0), 
		// Face X+, X- (Laterais) usam Lateral da Grama/Terra: (1, 0)
		new Vector2(1, 0), new Vector2(1, 0)
	},
	
	// Índice 2 (BL_DIRT) - Todas as faces usam Terra: (2, 0)
	{
		new Vector2(2, 0), new Vector2(2, 0), new Vector2(2, 0), 
		new Vector2(2, 0), new Vector2(2, 0), new Vector2(2, 0)
	},

	// Índice 3 (BL_STONE) - Todas as faces usam Pedra: (3, 0)
	{
		new Vector2(3, 0), new Vector2(3, 0), new Vector2(3, 0), 
		new Vector2(3, 0), new Vector2(3, 0), new Vector2(3, 0)
	}
};
	
	// Vertices da malha (um cubo)
	public static readonly Vector3[] VoxelVerts = new Vector3[8]
{
	new Vector3(0, 0, 0), // 0
	new Vector3(1, 0, 0), // 1
	new Vector3(1, 1, 0), // 2
	new Vector3(0, 1, 0), // 3
	new Vector3(0, 0, 1), // 4
	new Vector3(1, 0, 1), // 5
	new Vector3(1, 1, 1), // 6
	new Vector3(0, 1, 1)  // 7
};

// VoxelData.cs - VoxelFaceVerts (Os 4 vértices de cada face)
public static readonly int[] VoxelFaceVerts = new int[24] 
{
	// Z+ (Frente): Vértices 4, 5, 6, 7
	4, 5, 6, 7, 
	// Z- (Trás): Vértices 0, 1, 2, 3
	0, 3, 2, 1, // Nota: A ordem pode ser 0,1,2,3 para visualização correta
	// Y+ (Cima): Vértices 3, 7, 6, 2
	3, 7, 6, 2,
	// Y- (Baixo): Vértices 0, 1, 5, 4
	0, 1, 5, 4,
	// X+ (Direita): Vértices 1, 2, 6, 5
	1, 2, 6, 5,
	// X- (Esquerda): Vértices 4, 7, 3, 0
	4, 7, 3, 0
};
	
	// Coordenadas UV para texturização (usadas de forma simples aqui)
	public static readonly Vector2[] VoxelUvs = new Vector2[4]
	{
		new Vector2(0.0f, 0.0f),
		new Vector2(0.0f, 1.0f),
		new Vector2(1.0f, 0.0f),
		new Vector2(1.0f, 1.0f)
	};
	public static readonly Vector3[] faceChecks = new Vector3[6]
{
	new Vector3( 0, 0, 1),  // Z+ (Frente)
	new Vector3( 0, 0, -1), // Z- (Trás)
	new Vector3( 0, 1, 0),  // Y+ (Cima)
	new Vector3( 0, -1, 0), // Y- (Baixo)
	new Vector3( 1, 0, 0),  // X+ (Direita)
	new Vector3(-1, 0, 0)   // X- (Esquerda)
};
}
