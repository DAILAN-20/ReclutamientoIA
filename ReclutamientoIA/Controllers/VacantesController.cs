
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReclutamientoIA.Models;
using ReclutamientoIA.Data;

public class VacantesController : Controller
{
    private readonly ReclutamientoDbContext _context;

    public VacantesController(ReclutamientoDbContext context)
    {
        _context = context;
    }

    // GET: VACANTES
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Vacantes.ToListAsync());
    }

    // GET: VACANTES/Details/5
    public async Task<IActionResult> Details(int? idvacante)
    {
        if (idvacante == null)
        {
            return NotFound();
        }

        var vacante = await _context.Vacantes
            .FirstOrDefaultAsync(m => m.IdVacante == idvacante);
        if (vacante == null)
        {
            return NotFound();
        }

        return View(vacante);
    }

    // GET: VACANTES/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: VACANTES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("IdVacante,Titulo,Descripcion,Requisitos,Estado,FechaCreacion")] Vacante vacante)
    {
        if (ModelState.IsValid)
        {
            _context.Add(vacante);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(vacante);
    }

    // GET: VACANTES/Edit/5
    public async Task<IActionResult> Edit(int? idvacante)
    {
        if (idvacante == null)
        {
            return NotFound();
        }

        var vacante = await _context.Vacantes.FindAsync(idvacante);
        if (vacante == null)
        {
            return NotFound();
        }
        return View(vacante);
    }

    // POST: VACANTES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? idvacante, [Bind("IdVacante,Titulo,Descripcion,Requisitos,Estado,FechaCreacion")] Vacante vacante)
    {
        if (idvacante != vacante.IdVacante)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(vacante);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VacanteExists(vacante.IdVacante))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(vacante);
    }

    // GET: VACANTES/Delete/5
    public async Task<IActionResult> Delete(int? idvacante)
    {
        if (idvacante == null)
        {
            return NotFound();
        }

        var vacante = await _context.Vacantes
            .FirstOrDefaultAsync(m => m.IdVacante == idvacante);
        if (vacante == null)
        {
            return NotFound();
        }

        return View(vacante);
    }

    // POST: VACANTES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? idvacante)
    {
        var vacante = await _context.Vacantes.FindAsync(idvacante);
        if (vacante != null)
        {
            _context.Vacantes.Remove(vacante);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool VacanteExists(int? idvacante)
    {
        return _context.Vacantes.Any(e => e.IdVacante == idvacante);
    }
}
