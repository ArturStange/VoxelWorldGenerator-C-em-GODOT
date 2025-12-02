extends CharacterBody3D


const SPEED = 10.0
const JUMP_VELOCITY = 12
const MAX_REACH = 5.0
const BREAK_ACTION = "break_block"  #(Botão Esquerdo do Mouse)
const PLACE_ACTION = "place_block"  #(Botão Direito do Mouse)

var gravity = 35
var sensitivity = 0.002



@onready var camera_3d = $Camera3D
@onready var block_raycast: RayCast3D = $Camera3D/RayCast3D
@onready var world : Node3D = get_tree().get_first_node_in_group("world_maneger")

func _ready():
	Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)
	block_raycast.target_position = Vector3(0, 0, -MAX_REACH)
	block_raycast.enabled = true
func _unhandled_input(event):
	# 1. LÓGICA DE QUEBRAR E COLOCAR BLOCOS (Interação com o Mouse)
	if event.is_action_pressed(BREAK_ACTION):
		# Quebrar bloco (is_breaking = true)
		handle_block_interaction(true)
		# Consome o evento para que a lógica de movimento abaixo não o processe
		get_viewport().set_input_as_handled() 
	
	elif event.is_action_pressed(PLACE_ACTION):
		# Colocar bloco (is_breaking = false)
		handle_block_interaction(false)
		get_viewport().set_input_as_handled()
		
	# --- LÓGICA EXISTENTE DE MOVER A CÂMERA (Mouselook) ---
	
	# 2. SE FOR UM EVENTO DE MOVIMENTO DO MOUSE (e o bloco não foi interagido)
	# Geralmente, a rotação da câmera (mouselook) usa o tipo InputEventMouseMotion.
	elif event is InputEventMouseMotion:
		rotation.y = rotation.y - event.relative.x * sensitivity
		camera_3d.rotation.x = camera_3d.rotation.x - event.relative.y * sensitivity
		camera_3d.rotation.x = clamp(camera_3d.rotation.x, deg_to_rad(-70), deg_to_rad(80))
	
func _physics_process(delta: float) -> void:
	# Add the gravity.
	if not is_on_floor():
		velocity.y -= gravity * delta

	# Handle jump.
	if Input.is_action_just_pressed("jump") and is_on_floor():
		velocity.y = JUMP_VELOCITY

	# Get the input direction and handle the movement/deceleration.
	# As good practice, you should replace UI actions with custom gameplay actions.
	var input_dir := Input.get_vector("left", "right", "up", "down")
	var direction := (transform.basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()
	if direction:
		velocity.x = direction.x * SPEED
		velocity.z = direction.z * SPEED
	else:
		velocity.x = 0
		velocity.z = 0
	
	move_and_slide()

func handle_block_interaction(is_breaking: bool):
	# 1. Verificar colisão
	if block_raycast.is_colliding():
		var collision_point: Vector3 = block_raycast.get_collision_point()
		var collision_normal: Vector3 = block_raycast.get_collision_normal()
		
		# 2. Verificar se o nó World foi encontrado
		if world and world.has_method("ModifyVoxel"):
			# 3. Chamar a função de modificação no World.gd

			# Nota: No GDScript, a chamada é diretamente para a função em snake_case
			world.ModifyVoxel(collision_point, collision_normal, is_breaking)
		else:
			print("ERRO: Nó World ou método ModifyVoxel não encontrado!")
