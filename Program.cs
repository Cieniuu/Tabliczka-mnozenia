﻿using SDL2;
using System;

public enum Game_Run_State
{
	Running,
	Quit,
	Pause,
	Restart
};
public enum Game_Field_Type
{
	Grass,
	Number,
	Wall
};
	
public class Game_clock
{
	public float delta_time_s { get; private set; }
	public int target_fps;
    private UInt64 cpu_tick_frequency;

    public Game_clock()
	{
		cpu_tick_frequency = SDL.SDL_GetPerformanceFrequency();
	}

	public void Clock_Update_And_Wait(System.UInt64 tick_start)
	{
		double time_work_s = Get_Elapsed_Seconds_Here(tick_start);
        double target_s = 1.0 / (double)target_fps;
        double time_to_wait_s = target_s - time_work_s;

		if (time_to_wait_s > 0 && time_to_wait_s < target_s)
		{
			SDL.SDL_Delay((System.UInt32)(time_to_wait_s * 1000));
			time_to_wait_s = Get_Elapsed_Seconds_Here(tick_start);
			while (time_to_wait_s < target_s)
				time_to_wait_s = Get_Elapsed_Seconds_Here(tick_start);
		}

		delta_time_s = (float)Get_Elapsed_Seconds_Here(tick_start);
	}
	public double Get_Elapsed_Seconds_Here(System.UInt64 end)
	{
		return (double)(SDL.SDL_GetPerformanceCounter() - end) / cpu_tick_frequency;
	}
}
public class Game_window
{
	public System.IntPtr Window_handle { get; private set; }
	public int Width { get; private set; }
	public int Height { get; private set; }
	
	public Game_window(int w, int h)
	{
		Width = w;
		Height = h;
		SDL.SDL_SetHint(SDL.SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1");
		if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
		{
			Console.WriteLine($"There was an issue initilizing SDL. {SDL.SDL_GetError()}");
		}

		// Create a new window given a title, size, and passes it a flag indicating it should be shown.
		Window_handle = SDL.SDL_CreateWindow("Tabliczka mnozenia",
											SDL.SDL_WINDOWPOS_UNDEFINED,
											SDL.SDL_WINDOWPOS_UNDEFINED,
											Width,
											Height,
											SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);

		if (Window_handle == IntPtr.Zero)
		{
			Console.WriteLine($"There was an issue creating the window. {SDL.SDL_GetError()}");
		}
	}
}

public struct Number
{
	public int x;
    public int y;
	public int value;
    public Game_Field_Type type;
}

public class Game_state
{
	public Game_clock Clock { get; private set; }

	public Game_Run_State run_State;
	public int game_speed;
	public Game_Field_Type[,] Play_field { get; private set; }

	public Number[] numbers;

    public int expected_product;
	public int[] values;
    public int active_index;
    public int product;
	public int score;

    public int Play_field_size { get; private set; }
	public Random rand;

	public Game_state(int max_play_field_size)
	{
		Play_field = new Game_Field_Type[max_play_field_size, max_play_field_size];
		Play_field_size = max_play_field_size;

		Clock = new Game_clock();
        rand = new Random();
        numbers = new Number[6];
        values = new int[2];
    }

    private void Place_on_random_position(ref Number number)
	{
		if (Play_field[number.y, number.x] == Game_Field_Type.Grass)
		{
			do
			{
				number.x = rand.Next(0, Play_field_size);
				number.y = rand.Next(0, Play_field_size);
			} while (Play_field[number.y, number.x] != Game_Field_Type.Grass);

			number.value = rand.Next(1, 10);
            Play_field[number.y, number.x] = number.type;
        }
    }

	public void Spawn_Numbers()
    {
        foreach (ref Number number in numbers.AsSpan())
        {
            number.type = Game_Field_Type.Number;
            Place_on_random_position(ref number);
        }
    }

    public void Reset_Field_Except_Walls()
    {
        for (int y = 0; y < Play_field_size; y++)
        {
            for (int x = 0; x < Play_field_size; x++)
            {
                if (Play_field[y, x] != Game_Field_Type.Wall)
                    Play_field[y, x] = Game_Field_Type.Grass;
            }
        }
		foreach (ref Number number in numbers.AsSpan())
		{
			number.type = Game_Field_Type.Grass;
		}
	}
}
public struct Position
{
	public float x;
	public float y;
}

public struct Tile_position
{
    public int x;
    public int y;
}

public class Player
{
	public int vel_x;
	public int vel_y;
	public Position body;

	public Player()
	{
		vel_x = 0;
		vel_y = 0;
		body = new Position();
	}
	
	public void Move(float dT_s, float speed)
	{
		float new_head_pos_x = body.x + vel_x * speed;
		float new_head_pos_y = body.y + vel_y * speed;

		body.x = new_head_pos_x;
        body.y = new_head_pos_y;
    }
}

public class Render_context
{
	public Game_window Window { get; private set; }
    public System.IntPtr Ctx { get; private set; }


    public Render_context(int w, int h)
	{
		Window = new Game_window(w, h);
		Ctx = SDL.SDL_CreateRenderer(Window.Window_handle,
											   -1,
											   SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED |
											   SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);

		if (Ctx == IntPtr.Zero)
		{
			Console.WriteLine($"There was an issue creating the renderer. {SDL.SDL_GetError()}");
		}

		if (SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_PNG) == 0)
		{
			Console.WriteLine($"There was an issue initilizing SDL2_Image {SDL_image.IMG_GetError()}");
		}
	}
	public IntPtr Texture_load(string filename)
	{
		IntPtr texture;
		texture = SDL_image.IMG_LoadTexture(Ctx, filename);

		return texture;
	}
}

enum Button_ID
{
    None,
    Play,
    Quit
}

public static class Sprites
{
    public static dynamic player;
    public static SDL.SDL_Rect grass;
    public static SDL.SDL_Rect lava;
    public static dynamic numbers;
    public static dynamic button_play;
    public static dynamic button_quit;

    static Sprites()
    {
        player = new System.Dynamic.ExpandoObject();
        player.Down = new SDL.SDL_Rect  { x = 64 * 0, y = 192, w = 64, h = 64 };
        player.Up = new SDL.SDL_Rect    { x = 64 * 1, y = 192, w = 64, h = 64 };
        player.Right = new SDL.SDL_Rect { x = 64 * 2, y = 192, w = 64, h = 64 };
        player.Left = new SDL.SDL_Rect { x = 64 * 3, y = 192, w = 64, h = 64 };

        grass = new SDL.SDL_Rect    { x = 64,  y = 128, w = 64, h = 64 };
        lava = new SDL.SDL_Rect     { x = 0,   y = 128, w = 64, h = 64 }; 

        numbers = new System.Dynamic.ExpandoObject();
        numbers.One = new SDL.SDL_Rect      { x = 64 * 0, y = 64 * 0, w = 64, h = 64 };
        numbers.Two = new SDL.SDL_Rect      { x = 64 * 1, y = 64 * 0, w = 64, h = 64 };
        numbers.Three = new SDL.SDL_Rect    { x = 64 * 2, y = 64 * 0, w = 64, h = 64 };
        numbers.Four = new SDL.SDL_Rect     { x = 64 * 3, y = 64 * 0, w = 64, h = 64 };
        numbers.Five = new SDL.SDL_Rect     { x = 64 * 4, y = 64 * 0, w = 64, h = 64 };
        numbers.Six = new SDL.SDL_Rect      { x = 64 * 0, y = 64 * 1, w = 64, h = 64 };
        numbers.Seven = new SDL.SDL_Rect    { x = 64 * 1, y = 64 * 1, w = 64, h = 64 };
        numbers.Eight = new SDL.SDL_Rect    { x = 64 * 2, y = 64 * 1, w = 64, h = 64 };
        numbers.Nine = new SDL.SDL_Rect     { x = 64 * 3, y = 64 * 1, w = 64, h = 64 };
        numbers.Zero = new SDL.SDL_Rect     { x = 64 * 4, y = 64 * 1, w = 64, h = 64 };

        numbers.X = new SDL.SDL_Rect        { x = 64 * 4, y = 64 * 2, w = 64, h = 64 };

        button_play = new System.Dynamic.ExpandoObject();
        button_play.Non_active = new SDL.SDL_Rect   { x = 64 * 0, y = 64 * 4, w = 64 * 4, h = 64 * 2 };
        button_play.Active = new SDL.SDL_Rect       { x = 64 * 4, y = 64 * 4, w = 64 * 4, h = 64 * 2 };
        button_play.Clicked = new SDL.SDL_Rect      { x = 64 * 8, y = 64 * 4, w = 64 * 4, h = 64 * 2 };

        button_quit = new System.Dynamic.ExpandoObject();
        button_quit.Non_active = new SDL.SDL_Rect   { x = 64 * 0, y = 64 * 6, w = 64 * 4, h = 64 * 2 };
        button_quit.Active = new SDL.SDL_Rect       { x = 64 * 4, y = 64 * 6, w = 64 * 4, h = 64 * 2 };
        button_quit.Clicked = new SDL.SDL_Rect      { x = 64 * 8, y = 64 * 6, w = 64 * 4, h = 64 * 2 };
    }
}

public class Game
{
	public Game_state State { get; private set; }
	Player player;
    int tile_size;
    bool has_move_key_changed;
	
    Render_context renderer;
    int window_size;
	readonly IntPtr texture_atlas;
    int render_gameplay_offset;
    Button_ID active_button;

	public Game()
	{
        window_size = 915;
		renderer = new Render_context(window_size, window_size);
		player = new Player();
        State = new Game_state(15);
        State.Clock.target_fps = 60;
		has_move_key_changed = false;
        render_gameplay_offset = 128;
        texture_atlas = renderer.Texture_load("../../../textures/tabliczka_atlas.png");
        active_button = Button_ID.None;
		Gameplay_setup();

		tile_size = (renderer.Window.Width - render_gameplay_offset) / State.Play_field_size;
    }
    public void Gameplay_setup()
    {
        player.body.x = 7;
        player.body.y = 7;

        State.Reset_Field_Except_Walls();
        State.game_speed = 1;
        State.run_State = Game_Run_State.Pause;

        State.Spawn_Numbers();
        State.expected_product = Get_new_product();

        State.active_index = 0;
        State.score = 0;
    }

    public int Get_new_product()
    {
        return State.numbers[0].value * State.numbers[1].value;
    }
    public void Input_process()
	{
		SDL.SDL_Event event_sdl;
		SDL.SDL_PollEvent(out event_sdl);

		// Mouse input
		uint buttons = SDL.SDL_GetMouseState(out int mouse_x, out int mouse_y);
        int gameplay_pos_offset = render_gameplay_offset / 2;

		if ((buttons & SDL.SDL_BUTTON_LMASK) != 0)
		{   
            if (mouse_x >= gameplay_pos_offset && mouse_x < window_size - gameplay_pos_offset && 
                mouse_y >= gameplay_pos_offset && mouse_y < window_size - gameplay_pos_offset)
            {
                int tile_x = Get_Tile_Logical_Position(mouse_x);
                int tile_y = Get_Tile_Logical_Position(mouse_y);
                State.Play_field[tile_y, tile_x] = Game_Field_Type.Wall;
            }
		}
		if ((buttons & SDL.SDL_BUTTON_RMASK) != 0)
		{
            if (mouse_x >= gameplay_pos_offset && mouse_x < window_size - gameplay_pos_offset && 
                mouse_y >= gameplay_pos_offset && mouse_y < window_size - gameplay_pos_offset)
            {
                int tile_x = Get_Tile_Logical_Position(mouse_x);
                int tile_y = Get_Tile_Logical_Position(mouse_y);
                State.Play_field[tile_y, tile_x] = Game_Field_Type.Grass;
            }
		}

		// Keyboard input
		switch (event_sdl.type)
		{
			case SDL.SDL_EventType.SDL_QUIT:
				State.run_State = Game_Run_State.Quit;
				break;
			case SDL.SDL_EventType.SDL_KEYDOWN:
				if (event_sdl.key.keysym.sym == SDL.SDL_Keycode.SDLK_ESCAPE)
				{
					State.run_State = Game_Run_State.Quit;
				}
				if (event_sdl.key.keysym.sym == SDL.SDL_Keycode.SDLK_UP)
				{
					has_move_key_changed = !has_move_key_changed;
					player.vel_x = 0;
                    player.vel_y = -1;
                }
				if (event_sdl.key.keysym.sym == SDL.SDL_Keycode.SDLK_DOWN)
				{
					has_move_key_changed = !has_move_key_changed;
					player.vel_x = 0;
                    player.vel_y = 1;  
                }
				if (event_sdl.key.keysym.sym == SDL.SDL_Keycode.SDLK_RIGHT)
				{
					has_move_key_changed = !has_move_key_changed;
					player.vel_x = 1;
					player.vel_y = 0;	
				}
				if (event_sdl.key.keysym.sym == SDL.SDL_Keycode.SDLK_LEFT)
				{
					has_move_key_changed = !has_move_key_changed;
					player.vel_x = -1;
					player.vel_y = 0;
				}
                if (event_sdl.key.keysym.sym == SDL.SDL_Keycode.SDLK_p)
                {
                    if (State.run_State == Game_Run_State.Running)
                        State.run_State = Game_Run_State.Pause;
                    else if (State.run_State == Game_Run_State.Pause)
                        State.run_State = Game_Run_State.Running;
                }
                if (event_sdl.key.keysym.sym == SDL.SDL_Keycode.SDLK_c)
				{
					for (int y = 0; y < State.Play_field_size; y++)
					{
						for (int x = 0; x < State.Play_field_size; x++)
						{
							if (State.Play_field[y, x] == Game_Field_Type.Wall)
							{
								State.Play_field[y, x] = Game_Field_Type.Grass;
							}
						}
					}
				}
				break;
        }
    }
    public void Update()
	{
		if (has_move_key_changed == true)
		{
			player.Move(State.Clock.delta_time_s, State.game_speed);
			has_move_key_changed = !has_move_key_changed;
		}
		
        int bodyX = (int)player.body.x;
        int bodyY = (int)player.body.y;

        //kolizja z koncem mapy
        if (bodyX < 0 || bodyX >= State.Play_field_size)
        {
            State.run_State = Game_Run_State.Restart;
        }
        else if (bodyY < 0 || bodyY >= State.Play_field_size)
        {
            State.run_State = Game_Run_State.Restart;
        }
        //kolizja ze scianami
        else if (State.Play_field[bodyY, bodyX] == Game_Field_Type.Wall)
        {
            State.run_State = Game_Run_State.Restart;
        }

        //kolizja z numerem
        else if (State.Play_field[bodyY, bodyX] == Game_Field_Type.Number)
        {
            int i = Array.FindIndex(State.numbers, number => number.x == bodyX && number.y == bodyY);
            State.numbers[i].type = Game_Field_Type.Grass;
            State.Play_field[bodyY, bodyX] = Game_Field_Type.Grass;

            State.values[State.active_index] = State.numbers[i].value;
            if (State.active_index == 1)
            {
                State.product = State.values[0] * State.values[1];
                if (State.product == State.expected_product)
                {
                    State.score++;
                }
                else
                {
                    State.score--;
                }
            }
            State.active_index = (State.active_index + 1) % State.values.Length;

        }
        if (State.score == 10)
        {

        }
        else if (State.score < 0)
        {

        }
    }

    private static SDL.SDL_Rect Get_Number_Sprite(int number)
    {
        if (number >= 0 && number < 10)
        {
            switch (number)
            {
                case 1: return Sprites.numbers.One;
                case 2: return Sprites.numbers.Two;
                case 3: return Sprites.numbers.Three;
                case 4: return Sprites.numbers.Four;
                case 5: return Sprites.numbers.Five;
                case 6: return Sprites.numbers.Six;
                case 7: return Sprites.numbers.Seven;
                case 8: return Sprites.numbers.Eight;
                case 9: return Sprites.numbers.Nine;
                case 0: return Sprites.numbers.Zero;

                default:
                    break;
            }
        }
        else
        {
            return Sprites.numbers.X;
        }
        return Sprites.numbers.X;
    }
    public void Render()
	{
		SDL.SDL_SetRenderDrawColor(renderer.Ctx, 102, 0, 204, 255);
		SDL.SDL_RenderClear(renderer.Ctx);
        var texture_rect = Sprites.grass;
        var tile_rect = Get_Tile_Rendering_Position(0, 0);

        // Grass and wall back-filling
        for (int y = 0; y < State.Play_field_size; y++)
        {
            for (int x = 0; x < State.Play_field_size; x++)
            {
                tile_rect = Get_Tile_Rendering_Position(x, y);

                texture_rect = Sprites.grass;
                SDL.SDL_RenderCopy(renderer.Ctx, texture_atlas, ref texture_rect, ref tile_rect);

                if (State.Play_field[y, x] == Game_Field_Type.Wall)
                {
                    texture_rect = Sprites.lava;
                    SDL.SDL_RenderCopy(renderer.Ctx, texture_atlas, ref texture_rect, ref tile_rect);
                }

            }
        }

        // number render
        foreach (ref Number number in State.numbers.AsSpan())
        {
            tile_rect = Get_Tile_Rendering_Position(number.x, number.y);

            if (number.type == Game_Field_Type.Grass)
            {
                texture_rect = Sprites.grass;
            }
            else if (number.type == Game_Field_Type.Number)
            {
                texture_rect = Get_Number_Sprite(number.value);
            }

            SDL.SDL_RenderCopy(renderer.Ctx, texture_atlas, ref texture_rect, ref tile_rect);
        }

        // Player rendering
        tile_rect = Get_Tile_Rendering_Position((int)player.body.x, (int)player.body.y);

		Tile_position body_dir = new() { x = player.vel_x, y = player.vel_y };

		if (body_dir.x == 1 && body_dir.y == 0)
        {
            texture_rect = Sprites.player.Right;
        }
        else if (body_dir.x == -1 && body_dir.y == 0)
        {
            texture_rect = Sprites.player.Left;
        }
        else if (body_dir.x == 0 && body_dir.y == 1)
        {
            texture_rect = Sprites.player.Down;
        }
        else if (body_dir.x == 0 && body_dir.y == -1)
        {
            texture_rect = Sprites.player.Up;
        }
		else
        {
            texture_rect = Sprites.player.Up;
        }
        SDL.SDL_RenderCopy(renderer.Ctx, texture_atlas, ref texture_rect, ref tile_rect);

        // UI rendering

        //expected number render
        int num = State.expected_product;
        Span<int> digits = stackalloc int[3];
        int numer_of_digits = num == 0 ? 1 : (num > 0 ? 1 : 2) + (int)Math.Log10(Math.Abs((double)num));
        tile_rect.x = window_size / 2 - (numer_of_digits - 1) * (texture_rect.w / 2);
        tile_rect.y = 5;

        for (int i = 0; i < numer_of_digits; i++)
        {
            digits[i] = num % 10;
            num = num / 10;
        }

        for (int i = numer_of_digits - 1; i >= 0; i--)
        {
            texture_rect = Get_Number_Sprite(digits[i]);
            SDL.SDL_RenderCopy(renderer.Ctx, texture_atlas, ref texture_rect, ref tile_rect);
            tile_rect.x += 37;
        }

        // Render buttons
        SDL.SDL_Rect button_rect = new SDL.SDL_Rect
        {
            x = window_size / 2 - texture_rect.w * 4,
            y = window_size - 86,
            w = texture_rect.w * 4,
            h = 100
        };

        texture_rect = Sprites.button_quit.Non_active;
        Button(Button_ID.Quit, button_rect, texture_rect);
        if (active_button == Button_ID.Quit)
        {
            texture_rect = Sprites.button_quit.Active;
            Button(Button_ID.Quit, button_rect, texture_rect);
            if (Button(Button_ID.Quit, button_rect, texture_rect))
            {
                texture_rect = Sprites.button_quit.Clicked;
                Button(Button_ID.Quit, button_rect, texture_rect);
            }
        }

        texture_rect = Sprites.button_play.Non_active;
        button_rect.x += button_rect.w;
        Button(Button_ID.Play, button_rect, texture_rect);
        if (active_button == Button_ID.Play)
        {
            texture_rect = Sprites.button_play.Active;
            if ( Button(Button_ID.Play, button_rect, texture_rect) )
            {
                texture_rect = Sprites.button_play.Clicked; ;
                Button(Button_ID.Play, button_rect, texture_rect);
            }
        }

        SDL.SDL_RenderPresent(renderer.Ctx);
	}

    // Renders button at given tiles, sets status and returns true if clicked
    private bool Button(Button_ID id, SDL.SDL_Rect rect, SDL.SDL_Rect texture)
    {
        SDL.SDL_RenderCopy(renderer.Ctx, texture_atlas, ref texture, ref rect);
        uint mouse = SDL.SDL_GetMouseState(out int mouse_x, out int mouse_y);
        SDL.SDL_Point cursor = new SDL.SDL_Point { x = mouse_x, y = mouse_y };
   
        if (SDL.SDL_PointInRect(ref cursor, ref rect) == SDL.SDL_bool.SDL_TRUE)
        {
            // button hilighted
            active_button = id;
               
            if ( (mouse & SDL.SDL_BUTTON_LMASK) != 0)
            {
                // button clicked
                return true;
            }
        }
        else
            active_button = Button_ID.None;
        return false;
    }

	public void Terminate_SDL()
	{
		SDL.SDL_DestroyRenderer(renderer.Ctx);
		SDL.SDL_DestroyWindow(renderer.Window.Window_handle);
		SDL.SDL_Quit();
	}
    private SDL.SDL_Rect Get_Tile_Rendering_Position(int x, int y)
    {
        int tile_x = x * tile_size + (render_gameplay_offset / 2);
        int tile_y = y * tile_size + (render_gameplay_offset / 2);

        return new SDL.SDL_Rect { x = tile_x, y = tile_y, h = tile_size, w = tile_size };
    }

    private int Get_Tile_Logical_Position(int pixel_pos)
    {
        return Math.Clamp((pixel_pos - render_gameplay_offset / 2) / tile_size, 0, State.Play_field_size - 1);
    }
}
		
class Program
{
	static void Main()
	{
		Game game = new Game();

		// Main loop for the program
		while (game.State.run_State == Game_Run_State.Running || game.State.run_State == Game_Run_State.Pause)
		{
			System.UInt64 tick_start = SDL.SDL_GetPerformanceCounter();

			game.Input_process();

			if (game.State.run_State == Game_Run_State.Quit)
			{
				break;
			}
            if (game.State.run_State != Game_Run_State.Pause)
            {
                game.Update();
            }
            if (game.State.run_State == Game_Run_State.Restart)
			{
				game.Gameplay_setup();
			}

			game.Render();
			game.State.Clock.Clock_Update_And_Wait(tick_start);
		}
		game.Terminate_SDL();
	}
}