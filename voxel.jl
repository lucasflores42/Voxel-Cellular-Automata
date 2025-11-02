using Plots, LinearAlgebra, Printf

# -----------------------------------------------------------------------------
#                           SPH Particle Data Structure
# -----------------------------------------------------------------------------
mutable struct Particle
    position::Vector{Float64}
    velocity::Vector{Float64}
    acceleration::Vector{Float64}
    density::Float64
    pressure::Float64
    mass::Float64
    material::String
    radius::Float64
end

# -----------------------------------------------------------------------------
#                           Initialization
# -----------------------------------------------------------------------------
function initialize_particles()
    particles = Vector{Particle}(undef, num_particles)
    id = 1

    for i in 1:water_num_particles
        particles[id] = Particle(
            [0.02+0.5*rand(), (box_size-0.02)*rand(), 0.02+0.95*rand()],    # position
            [rand() * 0.0, rand() * 0.0, -rand()*0.5],                      # velocity
            [0.0, 0.0, gravity],                                            # acceleration
            1000.0,                                                         # density
            0.0,                                                            # pressure
            1.0,                                                            # mass
            "water",                                                          # material 
            0.02                                                            # radius
        )

        id += 1
    end

    for i in 1:sand_num_particles
        particles[id] = Particle(
            [0.02+0.2*rand(), 0.02+0.2*rand(), 0.02+0.95*rand()],    # position
            [rand() * 0.0, rand() * 0.0, -rand()*0.5],                      # velocity
            [0.0, 0.0, gravity],                                            # acceleration
            1000.0,                                                         # density
            0.0,                                                            # pressure
            1.0,                                                            # mass
            "sand",                                                          # material 
            0.02                                                            # radius
        )

        id += 1
    end
    
    for i in 1:air_num_particles
        particles[id] = Particle(
            [0.02+0.5*rand(), (box_size-0.02)*rand(), 0.02+0.95*rand()],    # position
            [rand() * 0.0, rand() * 0.0, -rand()*0.5],                      # velocity
            [0.0, 0.0, -gravity],                                            # acceleration
            1000.0,                                                         # density
            0.0,                                                            # pressure
            1.0,                                                            # mass
            "air",                                                          # material 
            0.02                                                            # radius
        )

        id += 1
    end

    return particles
end

# -----------------------------------------------------------------------------
#                       SPH Kernels
# -----------------------------------------------------------------------------
function kernel(r, h)
    q = r / h
    if q <= 1.0
        return (1.0 - 1.5*q*q + 0.75*q*q*q) / (π * h^3)
    elseif q <= 2.0
        return 0.25 * (2.0 - q)^3 / (π * h^3)
    else
        return 0.0
    end
end

# -----------------------------------------------------------------------------
#                       Density and Pressure Calculation
# -----------------------------------------------------------------------------
function calculate_density_pressure!(particle1,particles)

    particle1.density = 0.0
    
    # Calculate density
    for j in 1:length(particles)
        r_vec = particle1.position - particles[j].position
        r = norm(r_vec)
        
        particle1.density += particles[j].mass * kernel(r, smoothing_length)
    end
    
    # Calculate pressure 
    if particle1.material == "air"
        particle1.pressure = air_stiff_coef * ((particle1.density/air_target_density)^7 - 1)
    elseif particle1.material == "water"
        particle1.pressure = water_stiff_coef * ((particle1.density/water_target_density)^7 - 1)
    end
end


# -----------------------------------------------------------------------------
#                       Force Calculation 
# -----------------------------------------------------------------------------
function calculate_forces!(particles)

    for i in 1:length(particles)
        if particles[i].material == "water"

            calculate_density_pressure!(particles[i],particles)

            grad_pressure = zeros(3)
            laplacian_velocity = zeros(3)
            
            # Calculate pressure gradient and velocity Laplacian
            for j in 1:length(particles)
                if i == j
                    continue
                end
                
                r_vec = particles[i].position - particles[j].position
                r = norm(r_vec)
                
                if r > smoothing_length || r == 0
                    continue
                end
                
                # Kernel gradient calculation
                q = r / smoothing_length
                kernel_grad = zeros(3)
                if q <= 1.0
                    factor = (-3.0 + 2.25*q) / (π * smoothing_length^5)
                    kernel_grad = factor * r_vec
                elseif q <= 2.0
                    factor = -0.75 * (2.0 - q)^2 / (π * smoothing_length^5 * q)
                    kernel_grad = factor * r_vec
                end
                
                # Pressure gradient (Equation 6)
                pressure_term = (particles[i].pressure / (particles[i].density^2) + 
                            particles[j].pressure / (particles[j].density^2))
                grad_pressure += particles[j].mass * pressure_term * kernel_grad
                
                # Velocity Laplacian (Equation 8)
                v_ij = particles[i].velocity - particles[j].velocity
                dot_r_grad = dot(r_vec, kernel_grad)
                denominator = dot(r_vec, r_vec) + 0.01 * smoothing_length^2
                
                if denominator != 0
                    laplacian_velocity += 2.0 * (particles[j].mass / particles[j].density) * 
                                        v_ij * (dot_r_grad / denominator)
                end
            end
            
            # Pressure force (-∇P/ρ)
            Fi_pressure = -grad_pressure 
            
            # Viscosity force (ν∇²v)
            Fi_viscosity = particles[i].mass * water_viscosity_coef * laplacian_velocity
            
            # Gravity 
            Fi_gravity = particles[i].mass * [0.0, 0.0, gravity]
            
            # Total forces
            Fi = Fi_pressure + Fi_viscosity + Fi_gravity
            
            # Update velocity and position
            particles[i].velocity .+= (Fi / particles[i].mass) .* dt
            particles[i].position .+= particles[i].velocity .* dt

            #@printf "Particle %d: Pos=(%.3f, %.3f, %.3f) Vel=(%.3f, %.3f, %.3f) Density=%.2f Pressure=%.2f Fi_pressure=(%.3f, %.3f, %.3f) Fi_viscosity=(%.3f, %.3f, %.3f)\n" i particles[i].position[1] particles[i].position[2] particles[i].position[3] particles[i].velocity[1] particles[i].velocity[2] particles[i].velocity[3] particles[i].density particles[i].pressure Fi_pressure[1] Fi_pressure[2] Fi_pressure[3] Fi_viscosity[1] Fi_viscosity[2] Fi_viscosity[3]
        end

        if particles[i].material == "air"

            calculate_density_pressure!(particles[i],particles)

            grad_pressure = zeros(3)
            laplacian_velocity = zeros(3)
            
            # Calculate pressure gradient and velocity Laplacian
            for j in 1:length(particles)
                if i == j
                    continue
                end
                
                r_vec = particles[i].position - particles[j].position
                r = norm(r_vec)
                
                if r > smoothing_length || r == 0
                    continue
                end
                
                # Kernel gradient calculation
                q = r / smoothing_length
                kernel_grad = zeros(3)
                if q <= 1.0
                    factor = (-3.0 + 2.25*q) / (π * smoothing_length^5)
                    kernel_grad = factor * r_vec
                elseif q <= 2.0
                    factor = -0.75 * (2.0 - q)^2 / (π * smoothing_length^5 * q)
                    kernel_grad = factor * r_vec
                end
                
                # Pressure gradient (Equation 6)
                pressure_term = (particles[i].pressure / (particles[i].density^2) + 
                            particles[j].pressure / (particles[j].density^2))
                grad_pressure += particles[j].mass * pressure_term * kernel_grad
                
                # Velocity Laplacian (Equation 8)
                v_ij = particles[i].velocity - particles[j].velocity
                dot_r_grad = dot(r_vec, kernel_grad)
                denominator = dot(r_vec, r_vec) + 0.01 * smoothing_length^2
                
                if denominator != 0
                    laplacian_velocity += 2.0 * (particles[j].mass / particles[j].density) * 
                                        v_ij * (dot_r_grad / denominator)
                end
            end
            
            # Pressure force (-∇P/ρ)
            Fi_pressure = -grad_pressure 
            
            # Viscosity force (ν∇²v)
            Fi_viscosity = particles[i].mass * air_viscosity_coef * laplacian_velocity
            
            # Gravity 
            Fi_gravity = particles[i].mass * [0.0, 0.0, -gravity]
            
            # Total forces
            Fi = Fi_pressure + Fi_viscosity + Fi_gravity
            
            # Update velocity and position
            particles[i].velocity .+= (Fi / particles[i].mass) .* dt
            particles[i].position .+= particles[i].velocity .* dt

            #@printf "Particle %d: Pos=(%.3f, %.3f, %.3f) Vel=(%.3f, %.3f, %.3f) Density=%.2f Pressure=%.2f Fi_pressure=(%.3f, %.3f, %.3f) Fi_viscosity=(%.3f, %.3f, %.3f)\n" i particles[i].position[1] particles[i].position[2] particles[i].position[3] particles[i].velocity[1] particles[i].velocity[2] particles[i].velocity[3] particles[i].density particles[i].pressure Fi_pressure[1] Fi_pressure[2] Fi_pressure[3] Fi_viscosity[1] Fi_viscosity[2] Fi_viscosity[3]
        end

        if particles[i].material == "sand"
            
            # Gravity 
            Fi_gravity = particles[i].mass * [0.0, 0.0, gravity]
            
            # Total forces 
            Fi = Fi_gravity

            particles[i].velocity .+= (Fi / particles[i].mass) .* dt
            
            for j in i+1:length(particles)
                calculate_colision!(particles[i],particles[j])
            end
        end
    end

    calculate_sand_positions!(particles)
end
# -----------------------------------------------------------------------------
#                           Calculate colisions
# -----------------------------------------------------------------------------
function calculate_colision!(particle1,particle2)

    # elastic colision with restitution
    # m1 v1 + m2 v2 = m1 v1' + m2 v2'
    # C = |v2' - v1'|/|v2 - v1|

    r_vec = particle1.position - particle2.position
    r = norm(r_vec)

    if r < particle1.radius + particle2.radius && r >= 0.0001

        x1 = particle1.position
        x2 = particle2.position
        v1 = particle1.velocity
        v2 = particle2.velocity
        m1 = particle1.mass
        m2 = particle2.mass

        normal = (x1 - x2) / r
        particle1.position .+= (particle1.radius + particle2.radius - r) * normal /2
        particle2.position .-= (particle1.radius + particle2.radius - r) * normal /2

        r = particle1.radius + particle2.radius
        dv1 = - (1 + colision_restitution_coefficient) * m2 / (m1 + m2) * dot(v1 - v2, x1 - x2) * (x1 - x2) / r^2
        dv2 = - (1 + colision_restitution_coefficient) * m1 / (m1 + m2) * dot(v2 - v1, x2 - x1) * (x2 - x1) / r^2
        particle1.velocity .+= dv1
        particle2.velocity .+= dv2

        horizontal_damping = 0.01 
        particle1.velocity[1] *= horizontal_damping  
        particle1.velocity[2] *= horizontal_damping  
        particle2.velocity[1] *= horizontal_damping  
        particle2.velocity[2] *= horizontal_damping 

        #@printf "i=%d pos=%s vel=%s r=%.3f\n" i string(particles[i].position) string(particles[i].velocity) r 
    end
end
# -----------------------------------------------------------------------------
#                           Calculate positions
# -----------------------------------------------------------------------------
function calculate_sand_positions!(particles)
    for i in 1:length(particles)
        if particles[i].material == "sand"
         particles[i].position .+= particles[i].velocity * dt
        end
    end
end

# -----------------------------------------------------------------------------
#                       Boundary Conditions
# -----------------------------------------------------------------------------
function apply_boundary_conditions!(particles)
    
    for p in particles
        # X boundaries
        if p.position[1] < p.radius
            p.position[1] = p.radius
            p.velocity[1] *= -damping  
        elseif p.position[1] > box_size - p.radius
            p.position[1] = box_size - p.radius
            p.velocity[1] *= -damping  
        end
        
        # Y boundaries
        if p.position[2] < p.radius
            p.position[2] = p.radius
            p.velocity[2] *= -damping  
        elseif p.position[2] > box_size - p.radius
            p.position[2] = box_size - p.radius
            p.velocity[2] *= -damping  
        end
        
        # Z boundaries 
        if p.position[3] < p.radius
            p.position[3] = p.radius
            p.velocity[3] *= -damping  
        elseif p.position[3] > box_size - p.radius
            p.position[3] = box_size - p.radius
            p.velocity[3] *= -damping  
        end
    end
end


# -----------------------------------------------------------------------------
#                       Main Simulation Step
# -----------------------------------------------------------------------------
function simulate_step!(particles)
    calculate_forces!(particles)
    apply_boundary_conditions!(particles)
end

# -----------------------------------------------------------------------------
#                       Visualization
# -----------------------------------------------------------------------------
function visualize_sph(particles, step)
    x = [p.position[1] for p in particles]
    y = [p.position[2] for p in particles]
    z = [p.position[3] for p in particles]
    
    color_map = Dict(
        "sand" => :yellow,
        "water" => :blue,
        "air" => :gray
    )
    
    colors = [get(color_map, p.material, :red) for p in particles]
    
    plt = scatter3d(x, y, z,
            markersize=3,
            markercolor=colors,
            xlim=(0, box_size),
            ylim=(0, box_size),
            zlim=(0, box_size),
            title="SPH Simulation - Time $(round(step, digits=2))s",
            xlabel="Y", ylabel="X", zlabel="Z",
            legend=false,
            camera=(30, 30))
    
    return plt
end

# -----------------------------------------------------------------------------
#                           SPH Parameters
# -----------------------------------------------------------------------------
# world
const water_num_particles = 500
const sand_num_particles = 300
const air_num_particles = 100
const num_particles = water_num_particles + sand_num_particles + air_num_particles

const dt = 0.01
const box_size = 1.0
const damping = 0.0

const smoothing_length = 0.1

# water
const water_target_density = 1000.0
const water_stiff_coef = 100.
const water_viscosity_coef = 0.3

# air
const air_target_density = 1000.0
const air_stiff_coef = 100.
const air_viscosity_coef = 0.35

# colision
const colision_restitution_coefficient = 0.0
const gravity = -10.0

# -----------------------------------------------------------------------------
#                       Main Simulation
# -----------------------------------------------------------------------------
function main()
    particles = initialize_particles()
    
    t = 0.0
    frame_count = 0
    save_interval = max(1, round(Int, 0.01 / dt))
    
    while t < 10.0
        simulate_step!(particles)
        
        if frame_count % save_interval == 0  # Save every X frame
            plt = visualize_sph(particles, t)
            display(plt)
            #sleep(0.1)  
        end
        
        t += dt
        frame_count += 1
    end
end

main()